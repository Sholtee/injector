/********************************************************************************
* CreateInjector.cs                                                             *
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
        /// Creates a new <see cref="IInjector"/> instance from this container.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <returns>The newly created <see cref="IInjector"/> instance.</returns>
        /// <remarks><see cref="IInjector"/> represents also a scope.</remarks>
        /// <exception cref="InvalidOperationException">There are one or more abstract entries in the collection.</exception>
        public static IInjector CreateInjector(this IServiceContainer self) => new Injector
        (
            Ensure.Parameter.IsNotNull(self, nameof(self))
        );
    }
}