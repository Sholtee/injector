/********************************************************************************
* InterfaceInterceptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Proxy
{
    using Properties;
    using Internals;

    /// <summary>
    /// Provides the mechanism for intercepting interface method calls.
    /// </summary>
    /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
    public abstract class InterfaceInterceptor<TInterface> where TInterface: class
    {
        /// <summary>
        /// Signals that the original method should be called.
        /// </summary>
        /// <remarks>Internal, don't use it!</remarks>
        public static object CALL_TARGET = new object();

        /// <summary>
        /// Extracts the <see cref="MethodInfo"/> from the given expression.
        /// </summary>
        /// <param name="methodAccess">The expression to be process.</param>
        /// <returns>The extracted <see cref="MethodInfo"/> object.</returns>
        /// <remarks>This is an internal method, don't use it.</remarks>
        public static MethodInfo MethodAccess(Expression<Action> methodAccess) => ((MethodCallExpression) methodAccess.Body).Method;

        //
        // Ez itt NEM mukodik write-only property-kre
        //
/*
        protected static PropertyInfo PropertyAccess<TResult>(Expression<Func<TResult>> propertyAccess) => (PropertyInfo) ((MemberExpression) propertyAccess.Body).Member;
*/
        //
        // Ez mukodne viszont forditas ideju kifejezesek nem tartalmazhatnak ertekadast (lasd: http://blog.ashmind.com/2007/09/07/expression-tree-limitations-in-c-30/) 
        // tehat pl: "() => i.Prop = 0" hiaba helyes nem fog fordulni.
        //
/*
        protected static PropertyInfo PropertyAccess(Expression<Action> propertyAccess) => (PropertyInfo) ((MemberExpression) ((BinaryExpression) propertyAccess.Body).Left).Member;
*/
        //
        // Szoval marad a mersekelten szep megoldas (esemenyeket pedig amugy sem lehet kitalalni kifejezesek segitsegevel):
        //

        /// <summary>
        /// All the <typeparamref name="TInterface"/> properties.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, PropertyInfo> Properties = typeof(TInterface).ListInterfaceMembers(System.Reflection.TypeExtensions.GetProperties)
            .ToDictionary(prop => prop.Name);

        /// <summary>
        /// All the <typeparamref name="TInterface"/> events.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, EventInfo> Events = typeof(TInterface).ListInterfaceMembers(System.Reflection.TypeExtensions.GetEvents)
            .ToDictionary(ev => ev.Name);

        /// <summary>
        /// The target of this interceptor.
        /// </summary>
        public TInterface Target { get; }

        /// <summary>
        /// Creates a new <see cref="InterfaceInterceptor{TInterface}"/> instance against the given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target of this interceptor.</param>
        public InterfaceInterceptor(TInterface target) => Target = target;

        /// <summary>
        /// Called on proxy method invocation.
        /// </summary>
        /// <param name="method">The <typeparamref name="TInterface"/> method that was called</param>
        /// <param name="args">The arguments passed to the method.</param>
        /// <param name="extra">Extra info about the member from which the <paramref name="method"/> was extracted.</param>
        /// <returns>The object to return to the caller, or null for void methods.</returns>
        /// <remarks>The invocation will be forwarded to the <see cref="Target"/> if this method returns <see cref="CALL_TARGET"/>.</remarks>
        public virtual object Invoke(MethodInfo method, object[] args, MemberInfo extra) => Target != null ? CALL_TARGET : throw new InvalidOperationException(Resources.TARGET_IS_NULL);
    }
}
