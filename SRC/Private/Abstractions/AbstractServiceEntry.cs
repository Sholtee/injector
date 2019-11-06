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
    public class AbstractServiceEntry: Disposable, IServiceFactory, IServiceID
    {
        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The "id" of the service. Must be an interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        public AbstractServiceEntry(Type @interface, string name): this(@interface, name, null, null)
        { 
        }

        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The "id" of the service. Must be an interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="lifetime">The lifetime of the service.</param>
        /// <param name="owner">The owner of this entry.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        protected AbstractServiceEntry(Type @interface, string name, Lifetime? lifetime, IServiceContainer owner)
        {
            if (@interface == null)
                throw new ArgumentNullException(nameof(@interface));

            if (!@interface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(@interface));

            Interface = @interface;
            Name      = name;
            Lifetime  = lifetime;
            Owner     = owner;
        }

        #region Immutables
        /// <summary>
        /// The "id" of the service.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The lefiteime of the service (if it is present).
        /// </summary>
        public Lifetime? Lifetime { get; }

        /// <summary>
        /// The owner of this entry.
        /// </summary>
        public IServiceContainer Owner { get; }

        /// <summary>
        /// The name of the service (optional).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The implementation of the service (if it is present).
        /// </summary>
        public virtual Type Implementation => null;
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
        /// <param name="injectorFactory">Creates an injector instance from the <see cref="Owner"/> container.</param>
        /// <returns>The service instance.</returns>
        public virtual object GetService(Func<IInjector> injectorFactory) => throw new NotImplementedException();

        /// <summary>
        /// Copies this entry to a new collection.
        /// </summary>
        /// <param name="target">The target <see cref="IServiceContainer"/> to which we want to copy this entry.</param>
        public virtual AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            target.Add(this);
            return this;
        }

        /// <summary>
        /// Compares this entry with another one.
        /// </summary>
        /// <remarks>Entries having the same property values are considered equivalent.</remarks>
        public override bool Equals(object obj) => obj != null && (ReferenceEquals(this, obj) || obj.GetHashCode() == GetHashCode());

        /// <summary>
        /// Gets the hash code of this entry.
        /// </summary>
        /// <returns>The hash code of this entry.</returns>
        public override int GetHashCode() => new // muszaj anonimnak lennie
        {
            Owner,
            Interface,
            Name,
            Lifetime,
            Factory,
            Value,
            Implementation
        }.GetHashCode();
    }
}