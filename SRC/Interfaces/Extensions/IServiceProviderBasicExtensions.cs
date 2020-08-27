/********************************************************************************
* IServiceProviderBasicExtensions.cs                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines basic extensions for the <see cref="IServiceProvider"/> interface.
    /// </summary>
    public static class IServiceProviderBasicExtensions
    {
        /// <summary>
        /// Gets the service instance associated with the given interface.
        /// </summary>
        public static TInterface? GetService<TInterface>(this IServiceProvider self) where TInterface : class
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return (TInterface)self.GetService(typeof(TInterface));
        }
    }
}