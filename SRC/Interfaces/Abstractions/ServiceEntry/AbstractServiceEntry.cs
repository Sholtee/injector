/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Text;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives;
    using Properties;

    /// <summary>
    /// Describes an abstract service definition.
    /// </summary>
    public abstract class AbstractServiceEntry: IServiceDefinition
    {
        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The interface of the service.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        protected AbstractServiceEntry(Type @interface, string? name) : this(@interface, name, null, null)
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
        protected AbstractServiceEntry(Type @interface, string? name, Type? implementation, IServiceRegistry? owner)
        {
            Interface      = @interface ?? throw new ArgumentNullException(nameof(@interface));
            Owner          = owner;
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
        /// The (optional) implementation of the service.
        /// </summary>
        public Type? Implementation { get; }

        /// <summary>
        /// The owner of this entry.
        /// </summary>
        public IServiceRegistry? Owner { get; }

        /// <summary>
        /// If set, this property contains the accessor function that extracts the effective service instance from the value returned by the <see cref="Factory"/> function.
        /// </summary>
        /// <remarks>Disposal logic is alwais applied against the original value (returned by the <see cref="Factory"/> function).</remarks>
        public virtual Func<object, object>? ServiceAccess { get; }

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
        /// Describes the actual state of this entry.
        /// </summary>
        public ServiceEntryStates State { get; protected set; }
        #endregion

        /// <summary>
        /// Gets the previously created service instance.
        /// </summary>
        public abstract object GetSingleInstance();

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public abstract object CreateInstance(IInjector scope);

        /// <summary>
        /// Creates a copy from this entry.
        /// </summary>
        public abstract AbstractServiceEntry CopyTo(IServiceRegistry owner);

        /// <summary>
        /// See <see cref="object.ToString"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            const string NAME_PART = " - {0}: {1}";

            return new StringBuilder(this.FriendlyName())
                .AppendFormat(Resources.Culture, NAME_PART, nameof(Lifetime), Lifetime?.ToString() ?? "NULL")
                .AppendFormat(Resources.Culture, NAME_PART, nameof(Implementation), Implementation?.GetFriendlyName() ?? "NULL")           
                .ToString();
        }
    }
}