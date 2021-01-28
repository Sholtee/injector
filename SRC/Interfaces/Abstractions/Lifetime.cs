/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the lifetime of a service.
    /// </summary>
    public abstract class Lifetime
    {
        private static Lifetime? 
            FSingleton,
            FScoped,
            FTransient,
            FInstance;

        /// <summary>
        /// Services having singleton liftime are instantiated only once (in declaring <see cref="IServiceContainer"/>) on the first request and disposed automatically when their container is disposed.
        /// </summary>
        public static Lifetime Singleton { get => FSingleton ?? throw new NotSupportedException(); protected set => FSingleton = value; }

        /// <summary>
        /// Services having scoped liftime are instantiated only once (per <see cref="IInjector"/>) on the first request and disposed automatically when the parent <see cref="IInjector"/> is disposed.
        /// </summary>
        public static Lifetime Scoped { get => FScoped ?? throw new NotSupportedException(); protected set => FScoped = value; }

        /// <summary>
        /// Services having transient lifetime are instantiated on every request and released automatically when the parent <see cref="IInjector"/> is disposed.
        /// </summary>
        public static Lifetime Transient { get => FTransient ?? throw new NotSupportedException(); protected set => FTransient = value; }

        /// <summary>
        /// Services having instance lifetime behave like a constant value.
        /// </summary>
        public static Lifetime Instance { get => FInstance ?? throw new NotSupportedException(); protected set => FInstance = value; }

        /// <summary>
        /// Creates a service entry from the given <paramref name="implementation"/>.
        /// </summary>
        public virtual AbstractServiceEntry CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner) => throw new NotSupportedException();

        /// <summary>
        /// Creates a service entry from the given <paramref name="factory"/>.
        /// </summary>
        public virtual AbstractServiceEntry CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) => throw new NotSupportedException();

        /// <summary>
        /// Creates a service entry from the given <paramref name="value"/>.
        /// </summary>
        public virtual AbstractServiceEntry CreateFrom(Type iface, string? name, object value, bool externallyOwned, IServiceContainer owner) => throw new NotSupportedException();

        /// <summary>
        /// Returns true if the <paramref name="entry"/> was created by this factory.
        /// </summary>
        public abstract bool IsCompatible(AbstractServiceEntry entry);
    }
}
