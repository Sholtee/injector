/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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
        public AbstractServiceEntry(Type @interface, string? name, IServiceContainer owner): this(@interface, name, null, owner)
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

            if (implementation != null && !implementation.IsClass)
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
        #endregion

        #region Mutables
        /// <summary>
        /// The concrete factory. Don't use it directly.
        /// </summary>
        public Func<IInjector, Type, object>? Factory { get; protected set; }

        /// <summary>
        /// The previously created service instance. Don't use it directly.
        /// </summary>
        public IServiceReference? Instance { get; protected set; }
        #endregion

        /// <summary>
        /// Calls the <see cref="Factory"/> to set the service <see cref="Instance"/>.
        /// </summary>
        /// <param name="serviceReference">The <see cref="IServiceReference"/> of the service being created.</param>
        /// <param name="options">The options that control the instantiation process.</param>
        /// <returns>True on success false if the <see cref="Instance"/> had already been set previously.</returns>
        public virtual bool SetInstance(IServiceReference serviceReference, IReadOnlyDictionary<string, object> options) =>
            throw new NotImplementedException();

        /// <summary>
        /// Copies this entry to a new collection.
        /// </summary>
        /// <param name="target">The target <see cref="IServiceContainer"/> to which we want to copy this entry.</param>
        public virtual AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
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
        public override int GetHashCode() =>
#if NETSTANDARD2_0
            new // muszaj anonimnak lennie
            {
                Owner,
                Interface,
                Name,
                Factory,
                Instance?.Value,
                Implementation
            }.GetHashCode()
#else
            HashCode.Combine(Owner, Interface, Name, Factory, Instance?.Value, Implementation)
#endif
            ;

        /// <summary>
        /// See <see cref="object.ToString"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            const string 
                NAME_PART = " - {0}: {1}",
                NULL = nameof(NULL);

            return new StringBuilder(this.FriendlyName())
                .AppendFormat(Resources.Culture, NAME_PART, nameof(Implementation), Implementation?.ToString() ?? NULL)
                .AppendFormat(Resources.Culture, NAME_PART, nameof(Instance), Instance?.Value?.ToString() ?? NULL)
                .ToString();
        }

        /// <summary>
        /// Decrements the reference counter of the service <see cref="Instance"/>.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) Instance?.Release();

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Decrements the reference counter of the service <see cref="Instance"/>.
        /// </summary>
        protected override async ValueTask AsyncDispose()
        {
            if (Instance != null)
                await Instance.ReleaseAsync();

            //
            // Nem kell "base" hivas mert az a Dispose()-t hivna.
            //
        }
    }
}