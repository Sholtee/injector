/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives.Patterns;

    /// <summary>
    /// Describes the lifetime of a service.
    /// </summary>
    public abstract class Lifetime: ICloneable
    {
        private static Lifetime? 
            FSingleton,
            FScoped,
            FTransient,
            FPooled,
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
        /// Services having pooled lifetime are instantiated in a separate <see href="https://en.wikipedia.org/wiki/Object_pool_pattern">pool</see>. Every <see cref="IInjector"/> may request a service instance from the pool which is automatically resetted and returned when the parent <see cref="IInjector"/> is disposed.
        /// </summary>
        /// <remarks>Pooled services should implement the <see cref="IResettable"/> interface.</remarks>
        public static Lifetime Pooled { get => FPooled ?? throw new NotSupportedException(); protected set => FPooled = value; }

        /// <summary>
        /// Services having instance lifetime behave like a constant value.
        /// </summary>
        public static Lifetime Instance { get => FInstance ?? throw new NotSupportedException(); protected set => FInstance = value; }

        /// <summary>
        /// See <see cref="ICloneable.Clone"/>
        /// </summary>
        public virtual object Clone() => throw new NotImplementedException(); 

        /// <summary>
        /// Creates a service entry from the given <paramref name="implementation"/>.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner, params Func<object, Type, object>[] customConverters) => throw new NotSupportedException();

        /// <summary>
        /// Creates a service entry from the given <paramref name="implementation"/>.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters) => throw new NotSupportedException();

        /// <summary>
        /// Creates a service entry from the given <paramref name="factory"/>.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, params Func<object, Type, object>[] customConverters) => throw new NotSupportedException();

        /// <summary>
        /// Creates a service entry from the given <paramref name="value"/>.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, object value, bool externallyOwned, IServiceContainer owner, params Func<object, Type, object>[] customConverters) => throw new NotSupportedException();

        /// <summary>
        /// Returns true if the <paramref name="entry"/> was created by this factory.
        /// </summary>
        public abstract bool IsCompatible(AbstractServiceEntry entry);
    }
}
