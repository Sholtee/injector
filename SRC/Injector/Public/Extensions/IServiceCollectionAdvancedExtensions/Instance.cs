/********************************************************************************
* Instance.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;

    public static partial class IServiceCollectionAdvancedExtensions
    {
        /// <summary>
        /// Registers a pre-created instance. Useful to creating "constant" values (e.g. command-line arguments).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <paramref name="iface"/> interface.</param>
        public static IModifiedServiceCollection Instance(this IServiceCollection self, Type iface, string? name, object instance)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            return self.Register
            (
                //
                // Further validations are done by the created InstanceServiceEntry
                //

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