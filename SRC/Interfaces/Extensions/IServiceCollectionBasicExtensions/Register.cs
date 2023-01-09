/********************************************************************************
* Register.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Registers a set of services.
        /// </summary>
        public static IModifiedServiceCollection Register(this IServiceCollection self, IEnumerable<AbstractServiceEntry> entries)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (entries is null)
                throw new ArgumentNullException(nameof(entries));

            foreach (AbstractServiceEntry entry in entries)
                self.Add(entry);

            return (IModifiedServiceCollection) self;
        }

        /// <summary>
        /// Registers a set of services.
        /// </summary>
        public static IModifiedServiceCollection Register(this IServiceCollection self, params AbstractServiceEntry[] entries) => self.Register((IEnumerable<AbstractServiceEntry>) entries);
    }
}