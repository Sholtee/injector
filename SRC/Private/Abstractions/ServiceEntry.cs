/********************************************************************************
* ServiceEntry.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Stores a service definition.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    internal abstract class ServiceEntry: Disposable, IServiceFactory, IServiceInfo
    {
        protected ServiceEntry(Type @interface, Lifetime? lifetime, ServiceCollection owner)
        {
            Interface = @interface;
            Lifetime  = lifetime;
            Owner     = owner;
        }

        #region Immutables
        /// <summary>
        /// The interface of the service.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The lefiteime of the service (if present).
        /// </summary>
        public Lifetime? Lifetime { get; }

        /// <summary>
        /// The owner of this entry.
        /// </summary>
        public ServiceCollection Owner { get; }

        /// <summary>
        /// The implementation of the service (if present).
        /// </summary>
        public abstract Type Implementation { get; }

        public abstract bool IsService { get; }

        public abstract bool IsLazy { get; }

        public abstract bool IsFactory { get; }

        public abstract bool IsInstance { get; }       
        #endregion

        #region Mutables
        /// <summary>
        /// The concrete factory. Don't use it directly.
        /// </summary>
        public abstract Func<IInjector, Type, object> Factory { get; set; }

        /// <summary>
        /// The previously created service instance. Don't use it directly.
        /// </summary>
        public abstract object Value { get; } // hiaba csak getter, valtozhat
        #endregion

        /// <summary>
        /// Gets the service instance.
        /// </summary>
        /// <param name="injector">Containing injector.</param>
        /// <param name="iface">The service type to be queried. If null the <see cref="Interface"/> will be used</param>
        /// <returns>The service instance.</returns>
        public abstract object GetService(IInjector injector, Type iface = null);

        /// <summary>
        /// Copies this entry into a new collection.
        /// </summary>
        public abstract ServiceEntry CopyTo(ServiceCollection target);

        /// <summary>
        /// See <see cref="object.Equals(object)"/>
        /// </summary>
        public override bool Equals(object obj) => obj != null && (ReferenceEquals(this, obj) || obj.GetHashCode() == GetHashCode());

        /// <summary>
        /// See <see cref="object.GetHashCode"/>
        /// </summary>
        public override int GetHashCode() => new
        {
            Owner,
            Interface,
            Lifetime,
            Factory,
            Value,
            Implementation
        }.GetHashCode();
    }
}