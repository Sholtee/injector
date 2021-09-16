/********************************************************************************
* Instance.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Registers a pre-created instance. Useful to creating "constant" values (e.g. command-line arguments).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <paramref name="iface"/> interface.</param>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The container is responsible for disposing the entry.")]
        public static IModifiedServiceCollection Instance(this IServiceCollection self, Type iface, string? name, object instance)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            //
            // Tobbi parametert az InstanceServiceEntry konstruktora fogja ellenorizni.
            //

            return self.Register
            (
                Lifetime.Instance.CreateFrom(iface, name, instance)
            );
        }

        /// <summary>
        /// Registers a pre-created instance. Useful to creating "constant" values (e.g. command-line arguments).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <paramref name="iface"/> interface.</param>
        public static IModifiedServiceCollection Instance(this IServiceCollection self, Type iface, object instance)
            => self.Instance(iface, null, instance);

        /// <summary>
        /// Registers a pre-created instance. Useful when creating "constant" values (e.g. from command-line arguments).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="instance">The pre-created instance to be registered.</param>
        public static IModifiedServiceCollection Instance<TInterface>(this IServiceCollection self, string? name, TInterface instance) where TInterface: class 
            => self.Instance(typeof(TInterface), name, instance);

        /// <summary>
        /// Registers a pre-created instance. Useful when creating "constant" values (e.g. from command-line arguments).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="instance">The pre-created instance to be registered.</param>
        public static IModifiedServiceCollection Instance<TInterface>(this IServiceCollection self, TInterface instance) where TInterface: class
            => self.Instance(null, instance);
    }
}