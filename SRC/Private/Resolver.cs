/********************************************************************************
* Resolver.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal static class Resolver
    {
        public static Func<IInjector, object> Create(ConstructorInfo constructor)
        {
            MethodInfo ifaceGet = ((MethodCallExpression)((Expression<Action<IInjector>>) (i => i.Get(null))).Body).Method;

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

                    return Expression.Call(injector, ifaceGet, Expression.Constant(parameterType));
                },
                injector
            ).Compile();
        }
        
        public static Func<IInjector, IReadOnlyDictionary<string, object>, object> CreateExtended(ConstructorInfo constructor)
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
        }
    }
}
