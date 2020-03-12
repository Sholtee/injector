/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes an abstract service definition.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>

    [SuppressMessage("Security", "CA2119:Seal methods that satisfy private interfaces", Justification = "No security issues exist if the methods are overridden outside the assembly.")]
    public class AbstractServiceEntry: Disposable, IServiceDefinition, IServiceFactory
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
            Ensure.Parameter.IsNotNull(@interface, nameof(@interface));
            Ensure.Parameter.IsInterface(@interface, nameof(@interface));

            Interface = @interface;
            Name      = name;
            Lifetime  = lifetime;
            Owner     = owner;
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
        public string Name { get; }

        /// <summary>
        /// The (optional) lefiteime of the service.
        /// </summary>
        public Lifetime? Lifetime { get; }

        /// <summary>
        /// The owner of this entry.
        /// </summary>
        public IServiceContainer Owner { get; }

        /// <summary>
        /// The (optional) implementation of the service.
        /// </summary>
        public virtual Type Implementation { get; }
        #endregion

        #region Mutables
        /// <summary>
        /// The concrete factory. Don't use it directly.
        /// </summary>
        public Func<IInjector, Type, object> Factory { get; protected set; }

        /// <summary>
        /// The previously created service instance. Don't use it directly.
        /// </summary>
        public ServiceReference Instance { get; protected set; }
        #endregion

        /// <summary>
        /// Calls the <see cref="Factory"/> to set the service <see cref="Instance"/>.
        /// </summary>
        /// <param name="serviceReference">The <see cref="ServiceReference"/> of the service being created.</param>
        /// <param name="options">The options that control the instantiation process.</param>
        /// <returns>True on success false if the <see cref="Instance"/> had already been set previously.</returns>
        public virtual bool SetInstance(ServiceReference serviceReference, IReadOnlyDictionary<string, object> options) =>
            throw new InvalidOperationException(Resources.CANT_INSTANTIATE_ABSTRACTS);

        /// <summary>
        /// Copies this entry to a new collection.
        /// </summary>
        /// <param name="target">The target <see cref="IServiceContainer"/> to which we want to copy this entry.</param>
        public virtual AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            Ensure.Parameter.IsNotNull(target, nameof(target));
            Ensure.NotDisposed(this);

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
                Instance?.Value,
                Implementation
            }.GetHashCode()
#else
            HashCode.Combine(Owner, Interface, Name, Lifetime, Factory, Instance?.Value, Implementation)
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
                .AppendFormat(Resources.Culture, NAME_PART, nameof(Lifetime), Lifetime?.ToString() ?? NULL)
                .AppendFormat(Resources.Culture, NAME_PART, nameof(Implementation), Implementation?.ToString() ?? NULL)
                .AppendFormat(Resources.Culture, NAME_PART, nameof(Instance), Instance?.Value.ToString() ?? NULL)
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
        protected override async ValueTask AsyncDispose() =>
            await (Instance?.ReleaseAsync()).ConfigureAwait(false);

            //
            // Nem kell "base" hivas mert az a Dispose()-t hivna.
            //
    }
}