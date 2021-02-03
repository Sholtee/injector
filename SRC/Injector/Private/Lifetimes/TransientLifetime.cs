/********************************************************************************
* TransientLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class TransientLifetime : Lifetime
    {
        [ModuleInitializer]
        public static void Setup() => Transient = new TransientLifetime();

        public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => new TransientServiceEntry(iface, name, implementation, owner);

        public override AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => new TransientServiceEntry(iface, name, factory, owner);

        public override bool IsCompatible(AbstractServiceEntry entry) => entry is TransientServiceEntry;

        public override string ToString() => nameof(Transient);
    }
}
