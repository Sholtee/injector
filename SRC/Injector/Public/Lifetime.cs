/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    /// <summary>
    /// Describes the lifetime of a service.
    /// </summary>
    [SuppressMessage("Naming", "CA1724:Type names should not match namespaces")]
    public abstract class Lifetime: IServiceEntryFactory
    {
        /// <summary>
        /// See <see cref="IServiceEntryFactory.CreateFrom(Type, string?, Type, IServiceContainer)"/>
        /// </summary>
        public abstract AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner);

        /// <summary>
        /// See <see cref="IServiceEntryFactory.CreateFrom(Type, string?, Func{IInjector, Type, object}, IServiceContainer)"/>
        /// </summary>
        public abstract AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner);

        /// <summary>
        /// See <see cref="IServiceEntryFactory.IsCompatible(AbstractServiceEntry)"/>
        /// </summary>
        public abstract bool IsCompatible(AbstractServiceEntry entry);

        private sealed class SingletonLifetime : Lifetime
        {
            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => new SingletonServiceEntry(iface, name, implementation, owner);

            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => new SingletonServiceEntry(iface, name, factory, owner);

            public override bool IsCompatible(AbstractServiceEntry entry) => entry is SingletonServiceEntry;
        }

        /// <summary>
        /// Services having singleton liftime are instantiated only once (in declaring <see cref="IServiceContainer"/>) on the first request and disposed automatically when the container is disposed.
        /// </summary>
        public static Lifetime Singleton { get; } = new SingletonLifetime();

        private sealed class ScopedLifetime : Lifetime
        {
            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => new ScopedServiceEntry(iface, name, implementation, owner);

            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => new ScopedServiceEntry(iface, name, factory, owner);

            public override bool IsCompatible(AbstractServiceEntry entry) => entry is ScopedServiceEntry;
        }

        /// <summary>
        /// Services having scoped liftime are instantiated only once (per <see cref="IInjector"/>) on the first request and disposed automatically when the parent <see cref="IInjector"/> is disposed.
        /// </summary>
        public static Lifetime Scoped { get; } = new ScopedLifetime();

        private sealed class TransientLifetime : Lifetime
        {
            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => new TransientServiceEntry(iface, name, implementation, owner);

            public override AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => new TransientServiceEntry(iface, name, factory, owner);

            public override bool IsCompatible(AbstractServiceEntry entry) => entry is TransientServiceEntry;
        }

        /// <summary>
        /// Services having transient lifetime are instantiated on every request and released automatically when the parent <see cref="IInjector"/> is disposed.
        /// </summary>
        public static Lifetime Transient { get; } = new TransientLifetime();
    }
}
