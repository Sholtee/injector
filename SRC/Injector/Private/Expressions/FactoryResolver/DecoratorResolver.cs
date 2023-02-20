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
        public Expression<DecoratorDelegate> Resolve(Type iface, Type target, Type[] interceptorTypes, object?[] explicitArgs, IProxyEngine proxyEngine) => CreateActivator<DecoratorDelegate>
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
                        (interceptorType, i) => ResolveService
                        (
                            interceptorType.GetApplicableConstructor(),
                            paramz[0],
                            explicitArgs[i]
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
        public Expression<DecoratorDelegate> Resolve(Type iface, Type target, IEnumerable<IAspect> aspects, IProxyEngine proxyEngine)
        {
            return Resolve
            (
                iface,
                target,
                aspects.Select(GetInterceptor).ToArray(),
                aspects.Select(static aspect => aspect.ExplicitArgs).ToArray(),
                proxyEngine
            );

            static Type GetInterceptor(IAspect aspect)
            {
                Type interceptor = aspect.UnderlyingInterceptor;

                //
                // GetApplicableContructor() will do the rest of validations
                //

                if (!typeof(IInterfaceInterceptor).IsAssignableFrom(interceptor))
                    throw new InvalidOperationException(Resources.NOT_AN_INTERCEPTOR);

                return interceptor;
            }
        }

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
            IEnumerable<IAspect> aspects = GetAspects(iface);
            if (target != iface)
                aspects = aspects.Union(GetAspects(target));

            return aspects.Any()
                ? Resolve
                (
                    iface,
                    target,
                    aspects,
                    proxyEngine
                )
                : null;

            static IEnumerable<IAspect> GetAspects(Type type) => type
                .GetCustomAttributes()
                .OfType<IAspect>();
        }

        public DecoratorResolver(IReadOnlyList<IDependencyResolver>? resolvers) : base(resolvers) { }
    }
}