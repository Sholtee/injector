/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives.Patterns;
    using Properties;

    /// <summary>
    /// Describes an abstract service definition.
    /// </summary>
    public class AbstractServiceEntry: Disposable, IServiceDefinition
    {
        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The interface of the service.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="owner">The owner of this entry.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        public AbstractServiceEntry(Type @interface, string? name, IServiceContainer owner) : this(@interface, name, null, owner)
        {
        }

        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The interface of the service.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The (optional) implementation of the service.</param>
        /// <param name="owner">The owner of this entry.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        /// <exception cref="ArgumentException">The <paramref name="implementation"/> does not support the <paramref name="interface"/>.</exception>
        protected AbstractServiceEntry(Type @interface, string? name, Type? implementation, IServiceContainer owner)
        {
            Interface      = @interface ?? throw new ArgumentNullException(nameof(@interface));
            Owner          = owner ?? throw new ArgumentNullException(nameof(owner));
            Name           = name;
            Implementation = implementation;

            if (!@interface.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(@interface));

            //
            // Ha van implementacio akkor nem kell ellenorizni h megvalositja e az interface-t mivel:
            //   1) Ha meg is valositja, Proxy()-val ugy is el lehet rontani
            //   2) Epp ezert az Injector ellenorizni fogja a Factory hivas visszatereset
            //   3) Pl a Provider() hivas is "rossz" tipus regisztral
            //

            if (implementation is not null && !implementation.IsClass)
                throw new ArgumentException(Resources.NOT_A_CLASS, nameof(implementation));
        }

        #region Immutables
        /// <summary>
        /// The interface of the service.
        /// </summary>
        /// <remarks>A service is identified by its <see cref="Interface"/> and <see cref="Name"/>.</remarks>
        public Type Interface { get; }

        /// <summary>
        /// The (optional) name of the service.
        /// </summary>
        /// <remarks>A service is identified by its <see cref="Interface"/> and <see cref="Name"/>.</remarks>
        public string? Name { get; }

        /// <summary>
        /// The owner of this entry.
        /// </summary>
        public IServiceContainer Owner { get; }

        /// <summary>
        /// The (optional) implementation of the service.
        /// </summary>
        public Type? Implementation { get; }

        /// <summary>
        /// The related <see cref="Interfaces.Lifetime"/>.
        /// </summary>
        public virtual Lifetime? Lifetime { get; }

        /// <summary>
        /// Indicates whether this entry can be shared across injectors.
        /// </summary>
        public virtual bool IsShared { get; }
        #endregion

        #region Mutables
        /// <summary>
        /// The concrete factory. Don't use it directly.
        /// </summary>
        public Func<IInjector, Type, object>? Factory { get; protected set; }

        /// <summary>
        /// The previously created service instance(s) related to this entry. Don't use it directly.
        /// </summary>
        public virtual IReadOnlyCollection<IServiceReference> Instances { get; } = Array.Empty<IServiceReference>();

        /// <summary>
        /// Describes the actual state of this entry.
        /// </summary>
        public ServiceEntryStates State { get; protected set; }
        #endregion

        /// <summary>
        /// Calls the <see cref="Factory"/> to set an instance in the <see cref="Instances"/> list.
        /// </summary>
        /// <param name="serviceReference">The <see cref="IServiceReference"/> of the service being created.</param>
        /// <returns>Returns false if the entry was already built.</returns>
        public virtual bool SetInstance(IServiceReference serviceReference) =>
            throw new NotImplementedException();

        /// <summary>
        /// Returns the effective service instance from the given <paramref name="reference"/>. The <paramref name="reference"/> must be taken from this entry.
        /// </summary>
        public virtual object GetInstance(IServiceReference reference)
        {
            if (reference is null)
                throw new ArgumentNullException(nameof(reference));

            if (reference.RelatedServiceEntry != this)
                throw new ArgumentException(Resources.INCOMPATIBLE_REFERENCE);

            return reference.Value!;
        }

        /// <summary>
        /// Copies this entry to a new collection.
        /// </summary>
        /// <param name="target">The target <see cref="IServiceContainer"/> to which we want to copy this entry.</param>
        public virtual AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            
            CheckNotDisposed();

            target.Add(this);
            return this;
        }

        /// <summary>
        /// Compares this entry with another one.
        /// </summary>
        /// <remarks>Entries having the same property values are considered equivalent.</remarks>
        public override bool Equals(object obj) => ReferenceEquals(this, obj) || (obj is AbstractServiceEntry && obj.GetHashCode() == GetHashCode());

        /// <summary>
        /// Gets the hash code of this entry.
        /// </summary>
        /// <returns>The hash code of this entry.</returns>
        public override int GetHashCode()
        {
#if !NETSTANDARD2_0
            var hashCode = new HashCode();

            hashCode.Add(Owner);
            hashCode.Add(Interface);
            hashCode.Add(Name);
            hashCode.Add(Factory);
            hashCode.Add(Implementation);

            foreach (IServiceReference reference in Instances)
                hashCode.Add(GetInstance(reference));

            return hashCode.ToHashCode();
#else
            object current = new
            {
                Owner,
                Interface,
                Name,
                Factory,
                Implementation
            };

            foreach (IServiceReference reference in Instances)
                current = new
                {
                    Previous = current,
                    Instance = GetInstance(reference)
                };

            return current.GetHashCode();
#endif
        }

        /// <summary>
        /// See <see cref="object.ToString"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            const string NAME_PART = " - {0}: {1}";

            return new StringBuilder(this.FriendlyName())
                .AppendFormat(Resources.Culture, NAME_PART, nameof(Lifetime), Lifetime?.ToString() ?? "NULL")
                .AppendFormat(Resources.Culture, NAME_PART, nameof(Implementation), Implementation?.ToString() ?? "NULL")
                .AppendFormat(Resources.Culture, NAME_PART, nameof(Instances), Instances.Any() ? string.Join(", ", Instances.Select(GetInstance)) : "EMPTY")             
                .ToString();
        }

        /// <summary>
        /// Decrements the reference counter of the service <see cref="Instances"/>.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                foreach (IServiceReference instance in Instances)
                    instance.Release();

                State |= ServiceEntryStates.Disposed;
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Decrements the reference counter of the service <see cref="Instances"/>.
        /// </summary>
        protected override async ValueTask AsyncDispose()
        {
            await Task.WhenAll
            (
                Instances.Select(instance => instance.ReleaseAsync())
            );

            State |= ServiceEntryStates.Disposed;

            //
            // Nem kell "base" hivas mert az a Dispose()-t hivna.
            //
        }
    }
}