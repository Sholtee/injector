/********************************************************************************
* Register.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;

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
            {
                if (!self.Add(entry))
                    throw new InvalidOperationException(Resources.ENTRY_ALREADY_REGISTERED);
            }

            return (IModifiedServiceCollection) self;
        }

        /// <summary>
        /// Registers a set of services.
        /// </summary>
        public static IModifiedServiceCollection Register(this IServiceCollection self, params AbstractServiceEntry[] entries) => self.Register(entries);
    }
}