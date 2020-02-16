/********************************************************************************
* IServiceContainerExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Internals;

    public static partial class IServiceContainerExtensions
    {
        /// <summary>
        /// Creates a new <see cref="IServiceProvider"/> instance from this container.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="provider">The newly created <see cref="IServiceProvider"/> instance.</param>
        /// <returns>The scope of the newly created provider.</returns>
        /// <exception cref="InvalidOperationException">There are one or more abstract entries in the collection.</exception>
        public static IDisposable CreateProvider(this IServiceContainer self, out IServiceProvider provider) => (IDisposable) (provider = new ServiceProvider(self ?? throw new ArgumentNullException(nameof(self))));
    }
}