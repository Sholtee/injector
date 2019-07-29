/********************************************************************************
* IServiceCollection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a service collection.
    /// </summary>
    /// <remarks>This is an internal interface so it may change from version to version. Don't use it!</remarks>
    internal interface IServiceCollection: ICollection<ServiceEntry>
    {
        /// <summary>
        /// Gets the entry associated with the given interface.
        /// </summary>
        /// <param name="iface">The "id" of the entry. Must be an interface <see cref="Type"/>.</param>
        /// <returns>The stored <see cref="ServiceEntry"/> instance.</returns>
        /// <remarks>This method supports entry specialization which means after registering a generic entry you can query its (unregistered) closed pair by passing the closed interface <see cref="Type"/> to this function.</remarks>
        ServiceEntry QueryEntry(Type iface);
    }
}
