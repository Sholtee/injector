/********************************************************************************
* PermanentLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
#if false
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
        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner, params Func<object, Type, object>[] customConverters)
        {
            yield return new PermanentServiceEntry(iface, name, implementation, owner, customConverters);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters)
        {
            yield return new PermanentServiceEntry(iface, name, implementation, explicitArgs, owner, customConverters);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, params Func<object, Type, object>[] customConverters)
        {
            yield return new PermanentServiceEntry(iface, name, factory, owner, customConverters);
        }

        public override bool IsCompatible(AbstractServiceEntry entry) => entry is PermanentServiceEntry;

        public override string ToString() => "Permanent";
    }
}
#endif
