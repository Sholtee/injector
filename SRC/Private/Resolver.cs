/********************************************************************************
* Resolver.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal static class Resolver
    {
        private static readonly MethodInfo InjectorGet = ((MethodCallExpression) ((Expression<Action<IInjector>>) (i => i.Get(null))).Body).Method;

        public static Func<IInjector, object> Get(ConstructorInfo constructor) => Cache<ConstructorInfo, Func<IInjector, object>>.GetOrAdd(constructor, () =>
        { 
            //
            // (injector, type) => new Service((IDependency_1) injector.Get(typeof(IDependency_1)), ...)
            //

            ParameterExpression injector = Expression.Parameter(typeof(IInjector), nameof(injector));

            return constructor.ToLambda<Func<IInjector, object>>
            (
                (param, i) =>
                {
                    Type parameterType = param.ParameterType;
                    if (!parameterType.IsInterface)
                        throw new ArgumentException(Resources.INVALID_CONSTRUCTOR, nameof(constructor)); 

                    return Expression.Call(injector, InjectorGet, Expression.Constant(parameterType));
                },
                injector
            ).Compile();
        });

        public static Func<IInjector, object> Get(Type type) => Cache<Type, Func<IInjector, object>>.GetOrAdd(type, () => Get(type.GetApplicableConstructor()));

        public static Func<IInjector, IReadOnlyDictionary<string, object>, object> GetExtended(ConstructorInfo constructor) => Cache<ConstructorInfo, Func<IInjector, IReadOnlyDictionary<string, object>, object>>.GetOrAdd(constructor, () =>
        {
            Func<ParameterInfo, IInjector, IReadOnlyDictionary<string, object>, object> getArg = 
                (pi, i, ep) => ep.TryGetValue(pi.Name, out var value) 
                    ? value 
                    : i.Get(pi.ParameterType);

            //
            // (injector, explicitParamz) => new Service((IDependency_1) (explicitParamz[paramName] ||  injector.Get(typeof(IDependency_1))), ...)
            //

            ParameterExpression
                injector     = Expression.Parameter(typeof(IInjector), nameof(injector)),
                explicitArgs = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), nameof(explicitArgs));

            return constructor.ToLambda<Func<IInjector, IReadOnlyDictionary<string, object>, object>>
            (
                (p, i) => Expression.Invoke(Expression.Constant(getArg), Expression.Constant(p), injector, explicitArgs),
                injector,
                explicitArgs
            ).Compile();
        });

        public static Func<IInjector, IReadOnlyDictionary<string, object>, object> GetExtendned(Type type) => Cache<Type, Func<IInjector, IReadOnlyDictionary<string, object>, object>>.GetOrAdd(type, () => GetExtended(type.GetApplicableConstructor()));

        public static Func<IInjector, object> GetLazyFactory(Type iface) => Cache<Type, Func<IInjector, object>>.GetOrAdd(iface, () =>
        {
            Debug.Assert(iface.IsInterface);

            Type delegateType = typeof(Func<>).MakeGenericType(iface);

            //
            // injector => () => (iface) injector.Get(iface)
            //

            ParameterExpression injector = Expression.Parameter(typeof(IInjector), nameof(injector));

            Func<IInjector, Delegate> createValueFactory = Expression.Lambda<Func<IInjector, Delegate>>
            (
                Expression.Lambda
                (
                    delegateType,
                    Expression.Convert
                    (
                        Expression.Call
                        (
                            injector,
                            InjectorGet,
                            Expression.Constant(iface)
                        ),
                        iface
                    )
                ),
                injector
            ).Compile();

            //
            // injector => new Lazy<iface>(Func<iface>)
            //

            ConstructorInfo ctor = typeof(Lazy<>)
                .MakeGenericType(iface)
                .GetConstructor(new []{ delegateType });

            Debug.Assert(ctor != null);

            return i => ctor.Call(createValueFactory(i));
        });
    }
}
