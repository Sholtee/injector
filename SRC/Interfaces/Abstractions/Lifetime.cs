/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives.Patterns;

    /// <summary>
    /// Describes the lifetime of a service.
    /// </summary>
    [SuppressMessage("Design", "CA1724:Type names should not match namespaces")]
    [SuppressMessage("Design", "CA1036:Override methods on comparable types")]
    public abstract class Lifetime: IComparable<Lifetime>
    {
        private static readonly NotSupportedException NotSupported = new();

        private static Lifetime? 
            FSingleton,
            FScoped,
            FTransient,
            FPooled,
            FInstance;

        /// <summary>
        /// Services having singleton liftime are instantiated only once in the root scope (on the first request) and disposed automatically when the root is released.
        /// </summary>
        public static Lifetime Singleton { get => FSingleton ?? throw NotSupported; protected set => FSingleton = value; }

        /// <summary>
        /// Services having scoped liftime are instantiated only once (per scope) on the first request and disposed automatically when the parent scope is disposed.
        /// </summary>
        public static Lifetime Scoped { get => FScoped ?? throw NotSupported; protected set => FScoped = value; }

        /// <summary>
        /// Services having transient lifetime are instantiated on every request and released automatically when the parent scope is disposed.
        /// </summary>
        public static Lifetime Transient { get => FTransient ?? throw NotSupported; protected set => FTransient = value; }

        /// <summary>
        /// Services having pooled lifetime are instantiated in a separate <see href="https://en.wikipedia.org/wiki/Object_pool_pattern">pool</see>. Every <see cref="IInjector"/> may request a service instance from the pool which is automatically resetted and returned when the parent scope is disposed.
        /// </summary>
        /// <remarks>Pooled services should implement the <see cref="IResettable"/> interface.</remarks>
        public static Lifetime Pooled { get => FPooled ?? throw NotSupported; protected set => FPooled = value; }

        /// <summary>
        /// Services having instance lifetime behave like a constant value.
        /// </summary>
        public static Lifetime Instance { get => FInstance ?? throw NotSupported; protected set => FInstance = value; }

        /// <summary>
        /// See <see cref="IComparable{Lifetime}.CompareTo(Lifetime)"/>
        /// </summary>
        public abstract int CompareTo(Lifetime other);

        /// <summary>
        /// Creates one or more service entry against the given <paramref name="implementation"/>.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation) => throw NotSupported;

        /// <summary>
        /// Creates one or more service entry against the given <paramref name="implementation"/> using arbitrary constructor argument.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, object explicitArgs) => throw NotSupported;

        /// <summary>
        /// Creates one or more service entry against the given <paramref name="factory"/>.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory) => throw NotSupported;

        /// <summary>
        /// Creates one or more service entry against the given <paramref name="value"/>.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, object value) => throw NotSupported;

        /// <summary>
        /// Creates a copy from this instance using the given <paramref name="configuration"/>.
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        public virtual Lifetime Using(object configuration) => throw NotSupported;
    }
}
