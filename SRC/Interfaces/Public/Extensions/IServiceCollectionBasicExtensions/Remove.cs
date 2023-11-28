/********************************************************************************
* Remove.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;

    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Removes the <see cref="AbstractServiceEntry"/> associated with the given <paramref name="type"/> and (optional) <paramref name="key"/>.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static IServiceCollection Remove(this IServiceCollection self, Type type, object? key)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            IServiceId serviceId = new ServiceId(type, key);
            if (!self.Remove(serviceId))
                throw new ServiceNotFoundException
                (
                    string.Format
                    (
                        Resources.Culture,
                        Resources.SERVICE_NOT_FOUND,
                        serviceId
                    ),
                    null,
                    serviceId
                );

            return self;
        }

        /// <summary>
        /// Removes the <see cref="AbstractServiceEntry"/> associated with the given <paramref name="type"/>.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static IServiceCollection Remove(this IServiceCollection self, Type type) => self.Remove(type, null);

        /// <summary>
        /// Removes the <see cref="AbstractServiceEntry"/> associated with the given <typeparamref name="TType"/> and (optional) <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TType">The service type.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static IServiceCollection Remove<TType>(this IServiceCollection self, object? key) where TType : class => self.Remove(typeof(TType), key);

        /// <summary>
        /// Removes the <see cref="AbstractServiceEntry"/> associated with the given <typeparamref name="TType"/>.
        /// </summary>
        /// <typeparam name="TType">The service type.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static IServiceCollection Remove<TType>(this IServiceCollection self) where TType : class => self.Remove(typeof(TType));
    }
}