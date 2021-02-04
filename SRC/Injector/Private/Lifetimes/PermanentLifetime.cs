/********************************************************************************
* PermanentLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    //
    // !!!FIGYELEM!!! Csak belso hasznalatra, nem kell publikalni a Lifetime-ban
    //

    internal sealed partial class PermanentLifetime : Lifetime
    {
        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner)
        {
            yield return new PermanentServiceEntry(iface, name, implementation, owner);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner)
        {
            yield return new PermanentServiceEntry(iface, name, implementation, explicitArgs, owner);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner)
        {
            yield return new PermanentServiceEntry(iface, name, factory, owner);
        }

        public override bool IsCompatible(AbstractServiceEntry entry) => entry is PermanentServiceEntry;

        public override string ToString() => "Permanent";
    }
}
