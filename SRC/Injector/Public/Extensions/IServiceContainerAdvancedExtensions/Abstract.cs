/********************************************************************************
* Abstract.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    public static partial class IServiceContainerAdvancedExtensions
    {
        /// <summary>
        /// Registers an abstract service. It must be overridden in the child container(s).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The container itself.</returns>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The container is responsible for disposing the entry.")]
        public static IServiceContainer Abstract(this IServiceContainer self, Type iface, string? name = null)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            self.Add(new AbstractServiceEntry(iface, name, self));
            return self;
        }

        /// <summary>
        /// Registers an abstract service. It must be overridden in the child container(s).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Abstract<TInterface>(this IServiceContainer self, string? name = null) where TInterface: class
            => self.Abstract(typeof(TInterface), name);
    }
}