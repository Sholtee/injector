/********************************************************************************
* Instance.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    public static partial class IServiceContainerExtensions
    {
        /// <summary>
        /// Registers a pre-created instance. Useful to creating "constant" values (e.g. command-line arguments).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <paramref name="iface"/> interface.</param>
        /// <param name="releaseOnDispose">Whether the system should dispose the instance on container disposal or not.</param>
        /// <returns>The container itself.</returns>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The container is responsible for disposing the entry.")]
        public static IServiceContainer Instance(this IServiceContainer self, Type iface, string? name, object instance, bool releaseOnDispose = false)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            //
            // Tobbi parametert az InstanceServiceEntry konstruktora fogja ellenorizni.
            //

            self.Add(new InstanceServiceEntry(iface, name, instance, releaseOnDispose, self));
            return self;
        }

        /// <summary>
        /// Registers a pre-created instance. Useful to creating "constant" values (e.g. command-line arguments).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <paramref name="iface"/> interface.</param>
        /// <param name="releaseOnDispose">Whether the system should dispose the instance on container disposal or not.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Instance(this IServiceContainer self, Type iface, object instance, bool releaseOnDispose = false) => self.Instance(iface, null, instance, releaseOnDispose);

        /// <summary>
        /// Registers a pre-created instance. Useful when creating "constant" values (e.g. from command-line arguments).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="instance">The pre-created instance to be registered.</param>
        /// <param name="releaseOnDispose">Whether the system should dispose the instance on container disposal or not.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Instance<TInterface>(this IServiceContainer self, string? name, TInterface instance, bool releaseOnDispose = false) where TInterface: class 
            => self.Instance(typeof(TInterface), name, instance, releaseOnDispose);

        /// <summary>
        /// Registers a pre-created instance. Useful when creating "constant" values (e.g. from command-line arguments).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="instance">The pre-created instance to be registered.</param>
        /// <param name="releaseOnDispose">Whether the system should dispose the instance on container disposal or not.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Instance<TInterface>(this IServiceContainer self, TInterface instance, bool releaseOnDispose = false) where TInterface: class
            => self.Instance(null, instance, releaseOnDispose);
    }
}