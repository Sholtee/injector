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

    internal static class ServiceEntryExtensions
    {
        internal static IEnumerable<Expression<DecoratorDelegate>> ResolveDecorators(this AbstractServiceEntry self)
        {
            ServiceActivator serviceActivator = new(self.Options);

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
                            aspect => aspect.Factory ?? serviceActivator.ResolveInterceptorFactory
                            (
                                aspect.Interceptor ?? throw new InvalidOperationException
                                (
                                    string.Format(Culture, NOT_NULL, nameof(aspect.Interceptor))
                                ),
                                aspect.ExplicitArgs
                            )
                        );
                    if (delegates.Any())
                        //
                        // Bulk all the aspects into a single decorator
                        //

                        yield return serviceActivator.ResolveProxyDecorator
                        (
                            self.Type,
                            target,
                            delegates
                        );
                }
            }
        }

        internal static void ApplyInterceptors(this AbstractServiceEntry self, IEnumerable<(Type Interceptor, object? ExplicitArgs)> interceptors)
        {
            ServiceActivator serviceActivator = new(self.Options);

            self.Decorate
            (
                serviceActivator.ResolveProxyDecorator
                (
                    self.Type,

                    //
                    // Proxies registered by this way always target the service interface.
                    //

                    self.Type,
                    interceptors.Select
                    (
                        i => serviceActivator.ResolveInterceptorFactory
                        (
                            i.Interceptor ?? throw new InvalidOperationException
                            (
                                string.Format(Culture, NOT_NULL, nameof(i.Interceptor))
                            ),
                            i.ExplicitArgs
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