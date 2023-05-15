﻿/********************************************************************************
* DecoratorResolver.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    using static Properties.Resources;

    internal sealed class DecoratorResolver: FactoryResolverBase
    {
        /// <summary>
        /// <code>
        /// (injector, iface, current) => new GeneratedProxy // AspectAggregator&lt;TInterface, TTarget&gt;
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
        public static Expression<DecoratorDelegate> Resolve(Type iface, Type target, IProxyEngine? proxyEngine, params Expression<CreateInterceptorDelegate>[] factories)
        {
            proxyEngine ??= ProxyEngine.Instance;

            return CreateActivator<DecoratorDelegate>
            (
                paramz => proxyEngine.CreateActivatorExpression
                (
                    proxyEngine.CreateProxy(iface, target),
                    paramz[0], // scope
                    Expression.Convert(paramz[2], target),  // current
                    Expression.NewArrayInit
                    (
                        typeof(IInterfaceInterceptor),
                        factories.Select
                        (
                            fact => UnfoldLambdaExpressionVisitor.Unfold
                            (
                                fact,
                                paramz[0]  // scope
                            )
                        )
                    )
                )
            );
        }

        /// <summary>
        /// <code>injector => new Interceptor(injector.Get(...), ...)</code>
        /// </summary>
        public Expression<CreateInterceptorDelegate> ResolveInterceptorFactory(Type interceptor, object? explicitArgs)
        {
            //
            // GetApplicableContructor() will do the rest of validations
            //

            if (!typeof(IInterfaceInterceptor).IsAssignableFrom(interceptor))
                throw new InvalidOperationException(string.Format(NOT_AN_INTERCEPTOR, interceptor));

            return CreateActivator<CreateInterceptorDelegate>
            (
                interceptor.GetApplicableConstructor(),
                explicitArgs
            );
        }

        /// <summary>
        /// <code>injector => new Interceptor(injector.Get(...), ...)</code>
        /// </summary>
        public static Expression<CreateInterceptorDelegate> ResolveInterceptorFactory(Type interceptor, object? explicitArgs, IReadOnlyList<IDependencyResolver>? dependencyResolvers) =>
            new DecoratorResolver(dependencyResolvers).ResolveInterceptorFactory(interceptor, explicitArgs);

        public DecoratorResolver(IReadOnlyList<IDependencyResolver>? resolvers) : base(resolvers) { }
    }
}