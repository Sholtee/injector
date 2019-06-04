/********************************************************************************
* Resolver.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal static class Resolver
    {
        private static readonly MethodInfo IfaceGet = ((MethodCallExpression) ((Expression<Action<IInjector>>) (injector => injector.Get(null))).Body).Method;

        /// <summary>
        /// Creates a resolver function.
        /// </summary>
        public static Func<IInjector, Type, object> Create(ConstructorInfo constructor)
        {
            //
            // (injector, type) => new Service((IDependency_1) injector.Get(typeof(IDependency_1)), ...)
            //

            ParameterExpression injector = Expression.Parameter(typeof(IInjector), "injector");

            return constructor.ToLambda<Func<IInjector, Type, object>>
            (
                (parameterType, i) =>
                {
                    if (!parameterType.IsInterface)
                        throw new ArgumentException(Resources.INVALID_CONSTRUCTOR, nameof(constructor)); 

                    return Expression.Call(injector, IfaceGet, Expression.Constant(parameterType));
                },
                injector,

                //
                // Csak azert kell h a legyartott factory layout-ja stimmeljen.
                //

                Expression.Parameter(typeof(Type), "type")
            ).Compile();
        }
    }
}
