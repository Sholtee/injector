/********************************************************************************
* Register.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;

    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Registers a set of services.
        /// </summary>
        public static IModifiedServiceCollection Register(this IServiceCollection self!!, IEnumerable<AbstractServiceEntry> entries!!)
        {
            foreach (AbstractServiceEntry entry in entries)
            {
                //
                // Add() should throw if the entry is null
                //

                if (!self.Add(entry))
                    throw new ServiceAlreadyRegisteredException(Resources.SERVICE_ALREADY_REGISTERED);
            }

            return (IModifiedServiceCollection) self;
        }

        /// <summary>
        /// Registers a set of services.
        /// </summary>
        public static IModifiedServiceCollection Register(this IServiceCollection self, params AbstractServiceEntry[] entries) => self.Register((IEnumerable<AbstractServiceEntry>) entries);
    }
}