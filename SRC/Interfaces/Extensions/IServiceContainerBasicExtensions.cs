/********************************************************************************
* IServiceContainerBasicExtensions.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines basic extensions for the <see cref="IServiceContainer"/> interface.
    /// </summary>
    public static partial class IServiceContainerBasicExtensions
    {
        /// <summary>
        /// Gets the service entry associated with the given interface and name.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the entry.</param>
        /// <param name="mode">Options.</param>
        /// <returns>The requested service entry.</returns>
        /// <exception cref="ServiceNotFoundException">If the service could not be found.</exception>
        public static AbstractServiceEntry? Get<TInterface>(this IServiceContainer self, string? name, QueryModes mode = QueryModes.Default)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return self.Get(typeof(TInterface), name, mode);
        }

        /// <summary>
        /// Gets the service entry associated with the given interface.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="mode">Options.</param>
        /// <returns>The requested service entry.</returns>
        /// <exception cref="ServiceNotFoundException">If the service could not be found.</exception>
        public static AbstractServiceEntry? Get<TInterface>(this IServiceContainer self, QueryModes mode = QueryModes.Default) => self.Get<TInterface>(null, mode);
    }
}