/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
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
    
    using static Properties.Resources;

    using static ServiceActivator;

    internal static class ServiceEntryExtensions
    {
        internal static IEnumerable<Expression<DecoratorDelegate>> ResolveDecorators(this AbstractServiceEntry self)
        {
            ServiceOptions options = self.Options ?? ServiceOptions.Default;

            return self.Implementation is not null
                //
                // Return interceptors targeting the service implementation first
                //

                ? ResolveDecorators(self.Implementation, self.Type)
                : ResolveDecorators(self.Type);

            IEnumerable<Expression<DecoratorDelegate>> ResolveDecorators(params Type[] targets)
            {
                foreach (Type target in targets)
                {
                    IEnumerable<Expression<CreateInterceptorDelegate>> delegates = target
                        .GetCustomAttributes<AspectAttribute>(inherit: true)
                        .Select
                        (
                            aspect => aspect.Factory ?? ResolveInterceptorFactory
                            (
                                aspect.Interceptor ?? throw new InvalidOperationException
                                (
                                    string.Format(Culture, NOT_NULL, nameof(aspect.Interceptor))
                                ),
                                aspect.ExplicitArgs,
                                options.DependencyResolvers
                            )
                        );
                    if (!delegates.Any())
                        continue;

                    //
                    // Bulk all the aspects into a single decorator
                    //

                    yield return ResolveProxyDecorator
                    (
                        self.Type,
                        target,
                        options.ProxyEngine,
                        delegates
                    );
                }
            }
        }

        internal static void ApplyInterceptors(this AbstractServiceEntry entry, IEnumerable<(Type Interceptor, object? ExplicitArgs)> interceptors)
        {
            ServiceOptions options = entry.Options ?? ServiceOptions.Default;

            entry.Decorate
            (
                ResolveProxyDecorator
                (
                    entry.Type,

                    //
                    // Proxies registered by this way always target the service interface.
                    //

                    entry.Type,
                    options.ProxyEngine,
                    interceptors.Select
                    (
                        i => ResolveInterceptorFactory
                        (
                            i.Interceptor ?? throw new InvalidOperationException
                            (
                                string.Format(Culture, NOT_NULL, nameof(i.Interceptor))
                            ),
                            i.ExplicitArgs,
                            options.DependencyResolvers
                        )
                    )
                )
            );
        }

        /// <summary>
        /// Applies the aspects defined on service interface and implementation even if the <see cref="ServiceEntryFeatures.SupportsAspects"/> is not defined in <see cref="AbstractServiceEntry.Features"/>.
        /// </summary>
        internal static void ApplyAspects(this AbstractServiceEntry self)  // Since implementation targeting aspects must be registered first, this method can be called only once,
        {                                                                  // right after the entry construction -> do NOT expose it!
            foreach (Expression<DecoratorDelegate> decorator in self.ResolveDecorators())
            {
                self.Decorate(decorator);
            }
        }
    }
}