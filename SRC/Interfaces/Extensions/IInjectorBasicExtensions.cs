/********************************************************************************
* IInjectorBasicExtensions.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines basic extensions for the <see cref="IInjector"/> interface.
    /// </summary>
    public static class IInjectorBasicExtensions
    {
        /// <summary>
        /// Gets the service instance associated with the given interface and (optional) name.
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service to be resolved. It must be an interface.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        public static TInterface Get<TInterface>(this IInjector self, string? name = null) where TInterface : class
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return (TInterface) self.Get(typeof(TInterface), name);
        }

        /// <summary>
        /// Tries to get the service instance associated with the given interface and (optional) name.
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service to be resolved. It must be an interface.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The requested service instance if the resolution was successful, null otherwise.</returns>
        public static TInterface? TryGet<TInterface>(this IInjector self, string? name = null) where TInterface : class
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return (TInterface?) self.TryGet(typeof(TInterface), name);
        }
    }
}