/********************************************************************************
* DecoratorResolver.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal sealed class DecoratorResolver: FactoryResolverBase
    {
        /// <summary>
        /// <code>
        /// (injector, current) => new GeneratedProxy // AspectAggregator&lt;TInterface, TTarget&gt;
        /// (
        ///   (TTarget) current,
        ///   new IInterfaceInterceptor[]
        ///   {
        ///     new Interceptor_1(injector.Get&lt;IDep_1&gt;(), injector.Get&lt;IDep_2&gt;()),
        ///     new Interceptor_2(injector.Get&lt;IDep_3&gt;())
        ///   }
        /// ); 
        /// </code>
        /// </summary>
        public Expression<DecoratorDelegate> Resolve(Type iface, Type target, IEnumerable<Type> interceptorTypes, IProxyEngine proxyEngine) => CreateActivator<DecoratorDelegate>
        (
            paramz => proxyEngine.CreateActivatorExpression
            (
                proxyEngine.CreateProxy(iface, target),
                paramz[0],
                Expression.Convert(paramz[2], target),
                Expression.NewArrayInit
                (
                    typeof(IInterfaceInterceptor),
                    interceptorTypes.Select
                    (
                        interceptorType => ResolveService
                        (
                            interceptorType.GetApplicableConstructor(),
                            paramz[0],
                            null
                        )
                    )
                )
            )
        );

        /// <summary>
        /// <code>
        /// (injector, current) => new GeneratedProxy // AspectAggregator&lt;TInterface, TTarget&gt;
        /// (
        ///   (TTarget) current,
        ///   new IInterfaceInterceptor[]
        ///   {
        ///     new Interceptor_1(injector.Get&lt;IDep_1&gt;(), injector.Get&lt;IDep_2&gt;()),
        ///     new Interceptor_2(injector.Get&lt;IDep_3&gt;())
        ///   }
        /// ); 
        /// </code>
        /// </summary>
        public Expression<DecoratorDelegate>? ResolveForAspects(Type iface, Type target, IProxyEngine proxyEngine)
        {
            IEnumerable<Type> interceptors = GetInterceptors(iface);
            if (target != iface)
                interceptors = interceptors.Union(GetInterceptors(target));

            return interceptors.Any()
                ? Resolve
                (
                    iface,
                    target,
                    interceptors,
                    proxyEngine
                )
                : null;

            static IEnumerable<Type> GetInterceptors(Type type)
            {
                IEnumerable<Type> interceptors = type
                   .GetCustomAttributes()
                   .OfType<IAspect>()
                   .Select(static aspect => aspect.UnderlyingInterceptor);

                foreach (Type interceptor in interceptors)
                {
                    if (!typeof(IInterfaceInterceptor).IsAssignableFrom(interceptor))
                        throw new InvalidOperationException(Resources.NOT_AN_INTERCEPTOR);
                    yield return interceptor;
                }
            }
        }

        public DecoratorResolver(IReadOnlyList<IDependencyResolver>? resolvers) : base(resolvers) { }
    }
}