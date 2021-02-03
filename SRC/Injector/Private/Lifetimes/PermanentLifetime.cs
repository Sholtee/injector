/********************************************************************************
* PermanentLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    //
    // !!!FIGYELEM!!! Csak belso hasznalatra, nem kell publikalni a Lifetime-ban
    //

    internal sealed partial class PermanentLifetime : Lifetime
    {
        public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => new PermanentServiceEntry(iface, name, implementation, owner);

        public override AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => new PermanentServiceEntry(iface, name, factory, owner);

        public override bool IsCompatible(AbstractServiceEntry entry) => entry is PermanentServiceEntry;

        public override string ToString() => "Permanent";
    }
}
