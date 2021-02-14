/********************************************************************************
* CreateProvider.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    public static partial class IServiceContainerAdvancedExtensions
    {
        /// <summary>
        /// Creates a new <see cref="IServiceProvider"/> instance from this container.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="provider">The newly created <see cref="IServiceProvider"/> instance.</param>
        /// <param name="options">Custom options.</param>
        /// <returns>The scope of the newly created provider.</returns>
        /// <exception cref="InvalidOperationException">There are one or more abstract entries in the collection.</exception>
        public static Disposable CreateProvider(this IServiceContainer self, out IServiceProvider provider, IReadOnlyDictionary<string, object>? options = null)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            var result = new ServiceProvider(self, options);

            provider = result;
            return result;
        }
    }
}