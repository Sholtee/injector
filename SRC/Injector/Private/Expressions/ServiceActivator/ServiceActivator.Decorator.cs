﻿/********************************************************************************
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

    internal partial class ServiceActivator
    {
        /// <summary>
        /// <code>
        /// (injector, type, current) => new GeneratedProxy // AspectAggregator&lt;TInterface, TTarget&gt;
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
        public Expression<DecoratorDelegate> ResolveProxyDecorator(Type type, Type target, IEnumerable<Expression<CreateInterceptorDelegate>> factories)
        {
            //
            // Esure that the target is compatible (for instance Providers cannot have aspects)
            //

            if (!type.IsInterface || !type.IsAssignableFrom(target))
                throw new NotSupportedException(DECORATING_NOT_SUPPORTED);

            IProxyEngine proxyEngine = Options.ProxyEngine ?? ProxyEngine.Instance;

            return CreateActivator<DecoratorDelegate>
            (
                paramz => proxyEngine.CreateActivatorExpression
                (
                    proxyEngine.CreateProxy(type, target),
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
    }
}