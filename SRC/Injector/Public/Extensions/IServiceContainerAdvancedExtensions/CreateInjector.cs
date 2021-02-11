/********************************************************************************
* CreateInjector.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    public static partial class IServiceContainerAdvancedExtensions
    {
        /// <summary>
        /// Creates a new <see cref="IInjector"/> instance from this container.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="options">Custom options to be passed to <see cref="AbstractServiceEntry.SetInstance(IServiceReference, IReadOnlyDictionary{string, object})"/>.</param>
        /// <returns>The newly created <see cref="IInjector"/> instance.</returns>
        /// <remarks><see cref="IInjector"/> represents also a scope.</remarks>
        /// <exception cref="InvalidOperationException">There are one or more abstract entries in the collection.</exception>
        public static IInjector CreateInjector(this IServiceContainer self, IReadOnlyDictionary<string, object>? options = null) => new Injector
        (
            Ensure.Parameter.IsNotNull(self, nameof(self)),
            options
        );
    }
}