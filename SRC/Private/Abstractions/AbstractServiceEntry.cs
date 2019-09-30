/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes an abstract service definition.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    public class AbstractServiceEntry: Disposable, IServiceFactory
    {
        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        public AbstractServiceEntry(Type @interface, Lifetime? lifetime = null, IServiceContainer owner = null)
        {
            if (@interface == null)
                throw new ArgumentNullException(nameof(@interface));

            if (!@interface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(@interface));

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
        public IServiceContainer Owner { get; }

        /// <summary>
        /// The implementation of the service (if present).
        /// </summary>
        public virtual Type Implementation => null;

        /// <summary>
        /// Custom data.
        /// </summary>
        public virtual object UserData => null;
        #endregion

        #region Mutables
        /// <summary>
        /// The concrete factory. Don't use it directly.
        /// </summary>
        public virtual Func<IInjector, Type, object> Factory { get => null; set => throw new NotImplementedException(); }

        /// <summary>
        /// The previously created service instance. Don't use it directly.
        /// </summary>
        public virtual object Value => null;
        #endregion

        /// <summary>
        /// Gets the service instance.
        /// </summary>
        /// <param name="injector">Containing injector.</param>
        /// <param name="iface">The service type to be queried. If null the <see cref="Interface"/> will be used</param>
        /// <returns>The service instance.</returns>
        public virtual object GetService(IInjector injector, Type iface = null) => throw new NotImplementedException();

        /// <summary>
        /// Copies this entry into a new collection.
        /// </summary>
        public virtual AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.Add(this);
            return this;
        }

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