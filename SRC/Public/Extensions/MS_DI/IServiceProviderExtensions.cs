/********************************************************************************
* IServiceProviderExtensions.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Defines several handy extensions for the <see cref="IServiceProvider"/> interface.
    /// </summary>
    public static class IServiceProviderExtensions
    {
        /// <summary>
        /// Gets the service instance associated with the given interface.
        /// </summary>
        public static TInterface GetService<TInterface>(this IServiceProvider self) where TInterface: class => self != null 
            ? (TInterface) self.GetService(typeof(TInterface)) 
            : throw new ArgumentNullException(nameof(self));
    }
}
