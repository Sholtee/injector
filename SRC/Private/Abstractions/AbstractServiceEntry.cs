/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes an abstract service definition.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>

    [SuppressMessage("Security", "CA2119:Seal methods that satisfy private interfaces", Justification = "No security issues exist if the methods are overridden outside the assembly.")]
    public class AbstractServiceEntry: Disposable, IServiceFactory, IServiceID
    {
        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The interface of the service.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        public AbstractServiceEntry(Type @interface, string name): this(@interface, name, null, null)
        { 
        }

        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The interface of the service.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="lifetime">The lifetime of the service.</param>
        /// <param name="owner">The owner of this entry.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        internal protected AbstractServiceEntry(Type @interface, string name, Lifetime? lifetime, IServiceContainer owner)
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
        /// The interface of the service.
        /// </summary>
        /// <remarks>The service is identified by its <see cref="Interface"/> and <see cref="Name"/>.</remarks>
        public Type Interface { get; }

        /// <summary>
        /// The (optional) lefiteime of the service.
        /// </summary>
        public Lifetime? Lifetime { get; }

        /// <summary>
        /// The owner of this entry.
        /// </summary>
        public IServiceContainer Owner { get; }

        /// <summary>
        /// The (optional) name of the service.
        /// </summary>
        /// <remarks>The service is identified by its <see cref="Interface"/> and <see cref="Name"/>.</remarks>
        public string Name { get; }

        /// <summary>
        /// The (optional) implementation of the service.
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
        /// <param name="injector">The <see cref="IInjector"/> created from the <see cref="Owner"/> container.</param>
        /// <param name="serviceReference">The <see cref="AbstractServiceReference"/> of the service being created.</param>
        public virtual void GetService(IInjector injector, ref AbstractServiceReference serviceReference) => throw new NotImplementedException();

        /// <summary>
        /// Copies this entry to a new collection.
        /// </summary>
        /// <param name="target">The target <see cref="IServiceContainer"/> to which we want to copy this entry.</param>
        public virtual AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            CheckDisposed();

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
        public override int GetHashCode() =>
#if NETSTANDARD1_6 || NETSTANDARD2_0
            new // muszaj anonimnak lennie
            {
                Owner,
                Interface,
                Name,
                Lifetime,
                Factory,
                Value,
                Implementation
            }.GetHashCode()
#else
            HashCode.Combine(Owner, Interface, Name, Lifetime, Factory, Value, Implementation)
#endif
            ;
    }
}