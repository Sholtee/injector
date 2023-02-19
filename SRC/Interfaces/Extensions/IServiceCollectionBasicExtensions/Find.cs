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
        /// <exception cref="ServiceNotFoundException">The requested service could not be found.</exception>
        public static AbstractServiceEntry Find(this IServiceCollection self, Type iface, string? name)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            AbstractServiceEntry? entry = self.SingleOrDefault(svc => svc.Interface == iface && svc.Name == name);
            if (entry is null)
            {
                MissingServiceEntry missingService = new(iface, name);
                throw new ServiceNotFoundException
                (
                    string.Format
                    (
                        Resources.Culture,
                        Resources.SERVICE_NOT_FOUND,
                        missingService.ToString(shortForm: true)
                    ),
                    null,
                    missingService
                );
            }
            return entry;
        }

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">When the requested service could not be found.</exception>
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
        public static AbstractServiceEntry Find<TInterface>(this IServiceCollection self, string? name) where TInterface : class =>
            self.Find(typeof(TInterface), name);

        /// <summary>
        /// Tries to find a service descriptor (<see cref="AbstractServiceEntry"/>) in the given collection.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <returns>The service descriptor.</returns>
        /// <exception cref="ArgumentNullException">Some of the passed arguments is null.</exception>
        /// <exception cref="ServiceNotFoundException">When the requested service could not be found.</exception>
        public static AbstractServiceEntry Find<TInterface>(this IServiceCollection self) where TInterface : class =>
            self.Find<TInterface>(null);
    }
}