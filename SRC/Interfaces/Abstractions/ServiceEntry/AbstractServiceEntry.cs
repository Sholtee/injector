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
    /// Describes an abstract service entry.
    /// </summary>
    public abstract class AbstractServiceEntry
    {
        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The interface of the service.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        protected AbstractServiceEntry(Type @interface, string? name) : this(@interface, name, null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The interface of the service.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The (optional) implementation of the service.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        /// <exception cref="ArgumentException">The <paramref name="implementation"/> does not support the <paramref name="interface"/>.</exception>
        protected AbstractServiceEntry(Type @interface, string? name, Type? implementation)
        {
            Interface      = @interface ?? throw new ArgumentNullException(nameof(@interface));
            Name           = name;
            Implementation = implementation;

            if (!@interface.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(@interface));

            //
            // Ha van implementacio akkor nem kell ellenorizni h megvalositja e az interface-t mivel:
            //   1) Ha meg is valositja, Proxy()-val ugy is el lehet rontani
            //   2) Epp ezert az Injector ellenorizni fogja a Factory hivas visszatereset
            //   3) Pl a Provider() hivas is "rossz" tipust regisztral
            //

            if (implementation?.IsClass is false)
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
        /// The related <see cref="Interfaces.Lifetime"/>.
        /// </summary>
        public virtual Lifetime? Lifetime { get; }
        #endregion

        #region Mutables
        /// <summary>
        /// The concrete factory. Don't use it directly.
        /// </summary>
        public Func<IInjector, Type, object>? Factory { get; protected set; }

        /// <summary>
        /// Flags belong to this entry.
        /// </summary>
        public ServiceEntryFlags Flags { get; protected set; }
        #endregion

        /// <summary>
        /// Creates a new service instance.
        /// </summary>
        public abstract object CreateInstance(IInjector scope, out object? lifetime);

        /// <inheritdoc/>
        public override string ToString() => ToString(false); 

        /// <summary>
        /// Returns the string reprezentation of this entry.
        /// </summary>
        public string ToString(bool shortForm)
        {
            StringBuilder result = new(Interface.GetFriendlyName());
            
            if (Name is not null)
                result.Append($":{Name}");

            if (!shortForm)
            {
                const string
                    NAME_PART = " - {0}: {1}",
                    NULL = nameof(NULL);

                result
                    .AppendFormat(Resources.Culture, NAME_PART, nameof(Lifetime), Lifetime?.ToString() ?? NULL)
                    .AppendFormat(Resources.Culture, NAME_PART, nameof(Implementation), Implementation?.GetFriendlyName() ?? NULL);
            }

            return result.ToString();
        }

        /// <inheritdoc/>
        public sealed override int GetHashCode() => base.GetHashCode();
    }
}