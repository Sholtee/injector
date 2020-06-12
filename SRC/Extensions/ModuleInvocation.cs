/********************************************************************************
* ModuleInvocation.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Extensions
{
    using Interfaces;

    /// <summary>
    /// Adds a new alias to a member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class AliasAttribute: Attribute
    {
        /// <summary>
        /// The new name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Creates a new <see cref="AliasAttribute"/> instance.
        /// </summary>
        public AliasAttribute(string name) => Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Executes module methods by name.
    /// </summary>
    /// <param name="injector">The <see cref="IInjector"/> in which the module was registered.</param>
    /// <param name="ifaceId">The short name of the service interface (e.g.: IMyService).</param>
    /// <param name="methodId">The name of the interface emthod to be invoked.</param>
    /// <param name="args">The method arguments.</param>
    /// <remarks>This class is intended to be used in RPC servers where the invocation is represented by strings (e.g. comes from HTTP POST).</remarks>
    public delegate object ModuleInvocation(IInjector injector, string ifaceId, string methodId, params object[] args);

    /// <summary>
    /// Builds <see cref="ModuleInvocation"/>s.
    /// </summary>
    public class ModuleInvocationBuilder
    {
        #region Private
        private static MethodInfo InjectorGet = ((MethodCallExpression) ((Expression<Action<IInjector>>) (i => i.Get(null!, null))).Body).Method;

        //
        // {throw new Exception(...); return null;}
        //

        private static Expression Throw<TException>(Type[] argTypes, params Expression[] args) where TException: Exception => Expression.Block
        (
            typeof(object),
            Expression.Throw
            (
                Expression.New
                (
                    typeof(TException).GetConstructor(argTypes) ?? throw new MissingMethodException(typeof(TException).Name, "Ctor"),
                    args
                )
            ),
            Expression.Default(typeof(object))
        );

        private Expression CreateSwitch(ParameterExpression parameter, IEnumerable<(MemberInfo Member, Expression Body)> cases, Expression defaultBody) => Expression.Switch
        (
            switchValue: parameter,
            defaultBody,
            comparison: null, // default
            cases: cases.Select
            (
                @case => Expression.SwitchCase
                (
                    @case.Body,
                    Expression.Constant(GetMemberId(@case.Member))
                )
            )
        );

        private static IEnumerable<MethodInfo> GetAllInterfaceMethods(Type iface) =>
            //
            // A "BindingFlags.FlattenHierarchy" interface-ekre nem mukodik
            //

            iface
                .GetMethods(BindingFlags.Instance | BindingFlags.Public /* | BindingFlags.FlattenHierarchy*/)
                .Concat
                (
                    iface.GetInterfaces().SelectMany(GetAllInterfaceMethods)
                )

                //
                // IIface: IA, IB ahol IA: IC es IB: IC -> Distinct()
                //

                .Distinct();

        private Expression<ModuleInvocation> BuildExpression(IEnumerable<Type> interfaces) 
        {
            ParameterExpression
                injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                ifaceId  = Expression.Parameter(typeof(string),    nameof(ifaceId)),
                methodId = Expression.Parameter(typeof(string),    nameof(methodId)),
                args     = Expression.Parameter(typeof(object?[]), nameof(args));

            return Expression.Lambda<ModuleInvocation>
            (
                CreateSwitch
                (
                    parameter: ifaceId,
                    cases: interfaces.Select
                    (
                        iface =>
                        (
                            (MemberInfo) iface,
                            (Expression) CreateSwitch
                            (
                                parameter: methodId, 
                                cases: GetAllInterfaceMethods(iface).Select
                                (
                                    method => 
                                    (
                                        (MemberInfo) method, 
                                        (Expression) InvokeInjector(iface, method)
                                    )
                                ), 
                                defaultBody: Throw<MissingMethodException>(new[] { typeof(string), typeof(string) }, ifaceId, methodId)
                            )
                        )
                    ),
                    defaultBody: Throw<ServiceNotFoundException>(new[] { typeof(string) }, ifaceId)
                ),
                injector,
                ifaceId,
                methodId,
                args
            );


            Expression InvokeInjector(Type iface, MethodInfo method)
            {
                //
                // ((TInterface) injector.Get(typeof(TInterface), null)).Method((T0) arg[0], ..., (TN) argN)
                //

                Expression call = Expression.Call
                (
                    //
                    // (TInterface) injector.Get(typeof(TInterface), null)
                    //

                    instance: Expression.Convert
                    (
                        Expression.Call
                        (
                            injector,
                            InjectorGet,
                            Expression.Constant(iface),
                            Expression.Constant(null, typeof(string))
                        ),
                        iface
                    ),
                    method,

                    //
                    // (T0) arg[0], ..., (TN) argN 
                    // 

                    arguments: method.GetParameters().Select
                    (
                        (para, i) => Expression.Convert
                        (
                            Expression.ArrayAccess(args, Expression.Constant(i)),
                            para.ParameterType
                        )
                    )
                );

                return method.ReturnType != typeof(void)
                    //
                    // return (object) ((TInterface) injector.Get(typeof(TInterface), null)).Method((T0) arg[0], ..., (TN) argN)
                    //

                    ? (Expression) Expression.Convert(call, typeof(object))

                    //
                    // ((TInterface) injector.Get(typeof(TInterface), null)).Method((T0) arg[0], ..., (TN) argN);
                    // return null;
                    //

                    : Expression.Block(typeof(object), call, Expression.Default(typeof(object)));
            }

        }
        #endregion

        /// <summary>
        /// Gets the member name to be used in the execution process.
        /// </summary>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "'member' is never null")]
        protected virtual string GetMemberId(MemberInfo member) => member.GetCustomAttribute<AliasAttribute>(inherit: false)?.Name ?? member.Name;

        /// <summary>
        /// Builds a <see cref="ModuleInvocation"/> instance belongs to the specified <paramref name="interfaces"/>.
        /// </summary>
        public ModuleInvocation Build(params Type[] interfaces) 
        {
            if (interfaces == null)
                throw new ArgumentNullException(nameof(interfaces));

            if (interfaces.Any(iface => iface == null || !iface.IsInterface || iface.IsGenericTypeDefinition))
                throw new ArgumentException("", nameof(interfaces)); // TODO

            return BuildExpression(interfaces).Compile();
        }
    }
}
