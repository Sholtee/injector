/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Solti.Utils.DI
{
    using Internals;

    /// <summary>
    /// Encapsulates a service and its dependencies into a reference counted container.
    /// </summary>
    public class ServiceReference : DisposeByRefObject
    {
        private readonly ServiceReferenceCollection? FDependencies;

        private readonly WriteOnce FValue = new WriteOnce(strict: false);

        /// <summary>
        /// Creates a new <see cref="ServiceReference"/> instance.
        /// </summary>
        public ServiceReference(AbstractServiceEntry entry, IInjector injector)
        {
            RelatedServiceEntry = Ensure.Parameter.IsNotNull(entry, nameof(entry));
            RelatedInjector = Ensure.Parameter.IsNotNull(injector, nameof(injector));

            FDependencies = new ServiceReferenceCollection();
        }

        /// <summary>
        /// Creates a new <see cref="ServiceReference"/> instance with the given <paramref name="value"/>.
        /// </summary>
        public ServiceReference(AbstractServiceEntry entry, object value, bool externallyOwned)
        {
            RelatedServiceEntry = Ensure.Parameter.IsNotNull(entry, nameof(entry));
            Value = value; // set_Value() validal
            ExternallyOwned = externallyOwned;

            //
            // Elore definialt szerviz peldanynak nem lehet fuggosege.
            //
        }

        /// <summary>
        /// The related service entry.
        /// </summary>
        public AbstractServiceEntry RelatedServiceEntry { get; }

        /// <summary>
        /// The (optional) <see cref="IInjector"/> instance who created this reference.
        /// </summary>
        public IInjector? RelatedInjector { get; }

        /// <summary>
        /// The dependencies of the bound service. It can be used only if the <see cref="ExternallyOwned"/> is false.
        /// </summary>
        public ICollection<ServiceReference> Dependencies 
        {
            get 
            {
                Ensure.NotDisposed(this);

                return (ICollection<ServiceReference>?) FDependencies ?? Array.Empty<ServiceReference>();
            }
        }

        /// <summary>
        /// The bound service instance.
        /// </summary>
        public object? Value
        {
            get
            {
                Ensure.NotDisposed(this);
                return FValue.Value;
            }
            set 
            {
                //
                // Peldany tipusat ellenorizzuk mert a Factory(), Proxy() stb visszaadhat vicces dolgokat.
                //

                Ensure.Parameter.IsNotNull(value, nameof(value));
                Ensure.Type.IsTypeOf(RelatedServiceEntry.Interface, value!);
                Ensure.NotDisposed(this);

                FValue.Value = value;
            }
        }

        /// <summary>
        /// Returns true if the bound service will be disposed outside of this library false otherwise.
        /// </summary>
        public bool ExternallyOwned { get; }

        /// <summary>
        /// Disposes the bound service then decrements the reference counter of all its dependencies.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                if (!ExternallyOwned)
                {
                    Debug.WriteLine($"Disposing service: {RelatedServiceEntry}");

                    //
                    // Elso helyen szerepeljen h a fuggosegeket a Dispose()-ban meg hasznalni tudja a szerviz peldany.
                    //

                    (Value as IDisposable)?.Dispose();
                }

                FDependencies?.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Asynchronously disposes the bound service then decrements the reference counter of all its dependencies.
        /// </summary>
        protected async override ValueTask AsyncDispose()
        {
            if (!ExternallyOwned)
            {
                Debug.WriteLine($"Disposing service: {RelatedServiceEntry}");

                if (Value is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();

                //
                // Ha aszinkron nem lehet megprobaljuk szinkron is felszabaditani.
                //

                else if (Value is IDisposable disposable)
                    disposable.Dispose();
            }

            if (FDependencies != null)
                await FDependencies.DisposeAsync();

            //
            // Nem kell "base" hivas mert az a standard Dispose()-t hivna.
            //
        }
    }
}
