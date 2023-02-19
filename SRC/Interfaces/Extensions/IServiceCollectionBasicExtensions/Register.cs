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
        /// <remarks>Using the recepie methods (Service, Factory, etc) is more convenient than registering services directly.</remarks>
        public static IServiceCollection Register(this IServiceCollection self, IEnumerable<AbstractServiceEntry> entries)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (entries is null)
                throw new ArgumentNullException(nameof(entries));

            foreach (AbstractServiceEntry entry in entries)
            {
                self.Add(entry);
            }

            return self;
        }

        /// <summary>
        /// Registers a set of services.
        /// </summary>
        /// <remarks>Using the recepie methods (Service, Factory, etc) is more convenient than registering services directly.</remarks>
        public static IServiceCollection Register(this IServiceCollection self, params AbstractServiceEntry[] entries) => self.Register((IEnumerable<AbstractServiceEntry>) entries);
    }
}