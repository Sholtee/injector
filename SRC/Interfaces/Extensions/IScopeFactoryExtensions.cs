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
    /// Defines some extensions to the <see cref="IScopeFactory"/> interface.
    /// </summary>
    public static class IScopeFactoryExtensions
    {
        /// <summary>
        /// Creates a <see cref="IServiceProvider"/> instance representing the newly created scope.
        /// </summary>
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
