/********************************************************************************
* IServiceEntryFactory.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes an abstract service entry factory.
    /// </summary>
    public interface IServiceEntryFactory
    {
        /// <summary>
        /// Creates a service entry from the given <paramref name="implementation"/>.
        /// </summary>
        AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner);

        /// <summary>
        /// Creates a service entry from the given <paramref name="factory"/>.
        /// </summary>
        AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner);

        /// <summary>
        /// Returns true if the <paramref name="entry"/> was created by this factory.
        /// </summary>
        bool IsCompatible(AbstractServiceEntry entry);
    }
}
