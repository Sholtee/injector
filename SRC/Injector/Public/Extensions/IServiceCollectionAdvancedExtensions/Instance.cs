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
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once (with the given <paramref name="key"/>).</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <paramref name="type"/> interface.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Instance(this IServiceCollection self, Type type, object? key, object instance, ServiceOptions? options = null)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            return self.Register
            (
                //
                // Further validations are done by the created InstanceServiceEntry
                //

                Lifetime.Instance.CreateFrom(type, key, instance, options ?? ServiceOptions.Default)
            );
        }

        /// <summary>
        /// Registers a pre-created instance. Useful to creating "constant" values (e.g. command-line arguments).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once.</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <paramref name="type"/> interface.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Instance(this IServiceCollection self, Type type, object instance, ServiceOptions? options = null)
            => self.Instance(type, null, instance, options);

        /// <summary>
        /// Registers a pre-created instance. Useful when creating "constant" values (e.g. from command-line arguments).
        /// </summary>
        /// <typeparam name="TType">The service type to be registered. It can be registered only once (with the given <paramref name="key"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="instance">The pre-created instance to be registered.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Instance<TType>(this IServiceCollection self, object? key, TType instance, ServiceOptions? options = null) where TType: class 
            => self.Instance(typeof(TType), key, instance, options);

        /// <summary>
        /// Registers a pre-created instance. Useful when creating "constant" values (e.g. from command-line arguments).
        /// </summary>
        /// <typeparam name="TType">The service type to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="instance">The pre-created instance to be registered.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Instance<TType>(this IServiceCollection self, TType instance, ServiceOptions? options = null) where TType: class
            => self.Instance(key: null, instance, options);
    }
}