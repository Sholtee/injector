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

    internal static class ServiceEntryExtensions
    {
        internal static IEnumerable<Expression<DecoratorDelegate>> ResolveDecorators(this AbstractServiceEntry self)
        {
            ServiceOptions options = self.Options ?? ServiceOptions.Default;

            return self.Implementation is not null
                //
                // Return implementation targeting interceptors first
                //

                ? ResolveDecorators(self.Implementation, self.Interface)
                : ResolveDecorators(self.Interface);

            IEnumerable<Expression<DecoratorDelegate>> ResolveDecorators(params Type[] targets)
            {
                foreach (Type target in targets)
                {
                    Expression<CreateInterceptorDelegate>[] delegates = target
                        .GetCustomAttributes<AspectAttribute>(inherit: true)
                        .Select
                        (
                            aspect => aspect.Factory ?? DecoratorResolver.ResolveInterceptorFactory
                            (
                                aspect.Interceptor!,
                                aspect.ExplicitArgs,
                                options.DependencyResolvers
                            )
                        )
                        .ToArray();
                    if (delegates.Length is 0)
                        continue;

                    //
                    // Bulk all the aspects into a single decorator
                    //

                    yield return DecoratorResolver.Resolve
                    (
                        self.Interface,
                        target,
                        options.ProxyEngine,
                        delegates
                    );
                }
            }
        }

        /// <summary>
        /// Applies the aspects defined on service interface and implementation even if the <see cref="ServiceEntryFeatures.SupportsAspects"/> is not defined in <see cref="AbstractServiceEntry.Features"/>.
        /// </summary>
        internal static void ApplyAspects(this AbstractServiceEntry self)  // Since implementation targeting aspects must be registered first, this method can be called only
        {                                                                  // right after the entry construction -> do NOT expose it!
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            foreach (Expression<DecoratorDelegate> decorator in self.ResolveDecorators())
            {
                self.Decorate(decorator);
            }
        }
    }
}