/********************************************************************************
* ScopeLocal.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    public static partial class IServiceCollectionAdvancedExtensions
    {
        /// <summary>
        /// Registers a slot for a scope-local variable. Scope-locals are instances (for example the HTTP request object) provided by the current session.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once (with the given <paramref name="key"/>).</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <returns></returns>
        public static IServiceCollection SetupScopeLocal(this IServiceCollection self, Type type, object? key)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            object localKey = new
            {
                __type = type,
                __key = key
            };

            return self
                .Service<ScopeLocal>(localKey, Lifetime.Scoped)
                .Factory
                (
                    type,
                    key,
                    factoryExpr: (scope, _) => scope.Get<ScopeLocal>(localKey).Value,
                    Lifetime.Scoped,
                    ServiceOptions.Default with
                    {
                        DisposalMode = ServiceDisposalMode.Suppress
                    }
                );
        }

        /// <summary>
        /// Registers a slot for a scope-local variable. Scope-locals are instances (for example the HTTP request object) provided by the current session.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once.</param>
        /// <returns></returns>
        public static IServiceCollection SetupScopeLocal(this IServiceCollection self, Type type)
            => self.SetupScopeLocal(type, null);

        /// <summary>
        /// Registers a slot for a scope-local variable. Scope-locals are instances (for example the HTTP request object) provided by the current session.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <returns></returns>
        public static IServiceCollection SetupScopeLocal<TType>(this IServiceCollection self, object? key)
            => self.SetupScopeLocal(typeof(TType), key);

        /// <summary>
        /// Registers a slot for a scope-local variable. Scope-locals are instances (for example the HTTP request object) provided by the current session.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <returns></returns>
        public static IServiceCollection SetupScopeLocal<TType>(this IServiceCollection self)
            => self.SetupScopeLocal(typeof(TType), null);
    }
}
