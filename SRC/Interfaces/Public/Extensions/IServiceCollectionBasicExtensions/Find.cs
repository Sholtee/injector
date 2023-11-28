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
        /// <param name="type">The service type.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        public static AbstractServiceEntry? TryFind(this IServiceCollection self, Type type, object? key)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            return self.TryFind(new ServiceId(type, key));
        }

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        public static AbstractServiceEntry? TryFind(this IServiceCollection self, Type type) =>
            self.TryFind(type, null);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        public static AbstractServiceEntry? TryFind<TType>(this IServiceCollection self, object? key) where TType : class =>
            self.TryFind(typeof(TType), key);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">When the requested service could not be found.</exception>
        public static AbstractServiceEntry? TryFind<TType>(this IServiceCollection self) where TType : class =>
            self.TryFind<TType>(null);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">The requested service could not be found.</exception>
        public static AbstractServiceEntry Find(this IServiceCollection self, Type type, object? key)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            ServiceId serviceId = new(type, key);
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
        /// <param name="type">The service type.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">When the requested service could not be found.</exception>
        public static AbstractServiceEntry Find(this IServiceCollection self, Type type) =>
            self.Find(type, null);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">When the requested service could not be found.</exception>
        public static AbstractServiceEntry Find<TType>(this IServiceCollection self, object? key) where TType : class =>
            self.Find(typeof(TType), key);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">When the requested service could not be found.</exception>
        public static AbstractServiceEntry Find<TType>(this IServiceCollection self) where TType : class =>
            self.Find<TType>(null);
    }
}