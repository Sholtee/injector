/********************************************************************************
* LifetimeBase.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Represents the contract of service lifetime descriptors.
    /// </summary>
    [SuppressMessage("Design", "CA1724:Type names should not match namespaces")]
    [SuppressMessage("Design", "CA1036:Override methods on comparable types")]
    public abstract class LifetimeBase: IComparable<LifetimeBase>
    {
        private static readonly NotSupportedException NotSupported = new();

        /// <summary>
        /// See <see cref="IComparable{Lifetime}.CompareTo(Lifetime)"/>
        /// </summary>
        public abstract int CompareTo(LifetimeBase other);

        /// <summary>
        /// Creates one or more service entry against the given <paramref name="implementation"/>.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, Type implementation, ServiceOptions serviceOptions) => throw NotSupported;

        /// <summary>
        /// Creates one or more service entry against the given <paramref name="implementation"/> using arbitrary constructor arguments.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, Type implementation, object explicitArgs, ServiceOptions serviceOptions) => throw NotSupported;

        /// <summary>
        /// Creates one or more service entry against the given <paramref name="factory"/>.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, Expression<FactoryDelegate> factory, ServiceOptions serviceOptions) => throw NotSupported;

        /// <summary>
        /// Creates one or more service entry against the given <paramref name="value"/>.
        /// </summary>
        public virtual IEnumerable<AbstractServiceEntry> CreateFrom(Type type, object? key, object value, ServiceOptions serviceOptions) => throw NotSupported;

        /// <summary>
        /// Creates a copy from this instance using the given <paramref name="configuration"/>.
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        public virtual LifetimeBase Using(object configuration) => throw NotSupported;
    }
}
