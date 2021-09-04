/********************************************************************************
* IServiceRegistryExtensions.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Provides some extensions for the <see cref="IServiceRegistry"/> interface.
    /// </summary>
    public static class IServiceRegistryExtensions
    {
        /// <summary>
        /// Gets the servie entry associated with the given interface and name.
        /// </summary>
        public static AbstractServiceEntry? GetEntry<TInterface>(this IServiceRegistry self, string? name = null)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return self.GetEntry(typeof(TInterface), name);
        }
    }
}