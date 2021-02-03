/********************************************************************************
* SingletonLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class SingletonLifetime : Lifetime
    {
        [ModuleInitializer]
        public static void Setup() => Singleton = new SingletonLifetime();

        public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => new SingletonServiceEntry(iface, name, implementation, owner);

        public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) => new SingletonServiceEntry(iface, name, implementation, explicitArgs, owner);

        public override AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => new SingletonServiceEntry(iface, name, factory, owner);

        public override bool IsCompatible(AbstractServiceEntry entry) => entry is SingletonServiceEntry;

        public override string ToString() => nameof(Singleton);
    }
}
