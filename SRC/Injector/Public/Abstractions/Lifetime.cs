/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    /// <summary>
    /// Describes the lifetime of a service.
    /// </summary>
    public abstract class Lifetime: LifetimeBase, IHasPrecedence
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        protected Lifetime(int precedence) => Precedence = precedence;

        /// <summary>
        /// Services having singleton liftime are instantiated only once in the root scope (on the first request) and disposed automatically when the root is released.
        /// </summary>
        public static Lifetime Singleton { get; } = new SingletonLifetime();

        /// <summary>
        /// Services having scoped liftime are instantiated only once (per scope) on the first request and disposed automatically when the parent scope is disposed.
        /// </summary>
        public static Lifetime Scoped { get; } = new ScopedLifetime();

        /// <summary>
        /// Services having transient lifetime are instantiated on every request and released automatically when the parent scope is disposed.
        /// </summary>
        public static Lifetime Transient { get; } = new TransientLifetime();

        /// <summary>
        /// Services having pooled lifetime are instantiated in a separate <see href="https://en.wikipedia.org/wiki/Object_pool_pattern">pool</see>. Every <see cref="IInjector"/> may request a service instance from the pool which is automatically resetted and returned when the parent scope is disposed.
        /// </summary>
        /// <remarks>Pooled services should implement the <see cref="IResettable"/> interface.</remarks>
        public static Lifetime Pooled { get; } = new PooledLifetime();

        /// <summary>
        /// Services having instance lifetime behave like a constant value.
        /// </summary>
        internal static Lifetime Instance { get; } = new InstanceLifetime();

        /// <summary>
        /// Precendence of this lifetime.
        /// </summary>
        public int Precedence { get; }

        /// <summary>
        /// See <see cref="IComparable{LifetimeBase}.CompareTo(LifetimeBase)"/>
        /// </summary>
        public override int CompareTo(LifetimeBase other) => other is IHasPrecedence hasPrecedence
            ? Precedence - hasPrecedence.Precedence
            : other.CompareTo(this) * -1;
    }
}
