/********************************************************************************
* ServiceActivator.Decorator.cs                                                 *
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
    using static Interfaces.Properties.Resources;
    using static Properties.Resources;

    internal static partial class ServiceActivator
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
        public static Expression<DecoratorDelegate> ResolveProxyDecorator(Type iface, Type target, IProxyEngine? proxyEngine, IEnumerable<Expression<CreateInterceptorDelegate>> factories)
        {
            proxyEngine ??= ProxyEngine.Instance;

            //
            // Esure that the target is compatible (for instance Providers cannot have aspects)
            //

            if (!iface.IsAssignableFrom(target))
                throw new NotSupportedException(DECORATING_NOT_SUPPORTED);

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
        public static Expression<CreateInterceptorDelegate> ResolveInterceptorFactory(Type interceptor, object? explicitArgs, IReadOnlyList<IDependencyResolver>? resolvers)
        {
            //
            // GetApplicableContructor() will do the rest of validations
            //

            if (!typeof(IInterfaceInterceptor).IsAssignableFrom(interceptor))
                throw new InvalidOperationException(string.Format(NOT_AN_INTERCEPTOR, interceptor));

            return CreateActivator<CreateInterceptorDelegate>
            (
                interceptor.GetApplicableConstructor(),
                explicitArgs,
                resolvers ?? DefaultDependencyResolvers.Value
            );
        }
    }
}