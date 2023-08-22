/********************************************************************************
* IScopeFactoryExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives.Patterns;

    /// <summary>
    /// Defines some extensions for the <see cref="IScopeFactory"/> interface.
    /// </summary>
    public static class IScopeFactoryExtensions
    {
        /// <summary>
        /// Creates a new scope where the scope is represented by an <see cref="IServiceProvider"/> instane.
        /// </summary>
        /// <remarks>
        /// The returned disposable is responsible for releasing the scope instance:
        /// <code>
        /// using(IScopeFactory sf = ScopeFactory.Create(svcs => { }, ScopeOptions.Default with { SupportsServiceProvider = true } ))
        /// {
        ///     ...
        ///     using (sf.CreateScope(out IServiceProvider provider))
        ///     {
        ///         ...
        ///     }
        /// }
        /// </code>
        /// </remarks>
        public static IDisposableEx CreateScope(this IScopeFactory self, out IServiceProvider provider)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (!self.Options.SupportsServiceProvider)
                throw new NotSupportedException();

            IInjector injector = self.CreateScope();

            provider = (IServiceProvider) injector;

            return injector;
        }
    }
}
