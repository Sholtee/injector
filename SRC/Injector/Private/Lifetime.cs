/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class InjectorDotNetLifetime: Lifetime
    {
        [ModuleInitializer]
        public static void Setup()
        {
            Singleton = new SingletonLifetime();
            Scoped    = new ScopedLifetime();
            Transient = new TransientLifetime();
            Instance  = new InstanceLifetime();
        }

        private sealed class SingletonLifetime : Lifetime
        {
            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => new SingletonServiceEntry(iface, name, implementation, owner);
            
            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) => new SingletonServiceEntry(iface, name, implementation, explicitArgs, owner);

            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => new SingletonServiceEntry(iface, name, factory, owner);

            public override bool IsCompatible(AbstractServiceEntry entry) => entry is SingletonServiceEntry;

            public override string ToString() => nameof(Singleton);
        }

        private sealed class ScopedLifetime : Lifetime
        {
            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => new ScopedServiceEntry(iface, name, implementation, owner);

            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) => new ScopedServiceEntry(iface, name, implementation, explicitArgs, owner);

            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => new ScopedServiceEntry(iface, name, factory, owner);

            public override bool IsCompatible(AbstractServiceEntry entry) => entry is ScopedServiceEntry;

            public override string ToString() => nameof(Scoped);
        }

        private sealed class TransientLifetime : Lifetime
        {
            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => new TransientServiceEntry(iface, name, implementation, owner);

            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) => new TransientServiceEntry(iface, name, implementation, explicitArgs, owner);

            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => new TransientServiceEntry(iface, name, factory, owner);

            public override bool IsCompatible(AbstractServiceEntry entry) => entry is TransientServiceEntry;

            public override string ToString() => nameof(Transient);
        }

        private sealed class InstanceLifetime : Lifetime 
        {
            public override AbstractServiceEntry CreateFrom(Type iface, string? name, object value, bool externallyOwned, IServiceContainer owner) => new InstanceServiceEntry(iface, name, value, externallyOwned, owner);

            public override bool IsCompatible(AbstractServiceEntry entry) => entry is InstanceServiceEntry;

            public override string ToString() => nameof(Instance);
        }
    }
}
