/********************************************************************************
* Find.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;

    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <param name="name">The (optional) service name.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static AbstractServiceEntry? TryFind(this IServiceCollection self, Type iface, object? name)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            return self.TryFind(new ServiceId(iface, name));
        }

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static AbstractServiceEntry? TryFind(this IServiceCollection self, Type iface) =>
            self.TryFind(iface, null);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) service name.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static AbstractServiceEntry? TryFind<TInterface>(this IServiceCollection self, object? name) where TInterface : class =>
            self.TryFind(typeof(TInterface), name);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">When the requested service could not be found.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static AbstractServiceEntry? TryFind<TInterface>(this IServiceCollection self) where TInterface : class =>
            self.TryFind<TInterface>(null);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <param name="name">The (optional) service name.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">The requested service could not be found.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static AbstractServiceEntry Find(this IServiceCollection self, Type iface, object? name)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            ServiceId serviceId = new(iface, name);
            return self.TryFind(serviceId) ??  throw new ServiceNotFoundException
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
        }

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">When the requested service could not be found.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static AbstractServiceEntry Find(this IServiceCollection self, Type iface) =>
            self.Find(iface, null);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) service name.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">When the requested service could not be found.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static AbstractServiceEntry Find<TInterface>(this IServiceCollection self, object? name) where TInterface : class =>
            self.Find(typeof(TInterface), name);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">When the requested service could not be found.</exception>
        /// <remarks>This method uses linear search so should be avoided in perfomance critical places.</remarks>
        public static AbstractServiceEntry Find<TInterface>(this IServiceCollection self) where TInterface : class =>
            self.Find<TInterface>(null);
    }
}