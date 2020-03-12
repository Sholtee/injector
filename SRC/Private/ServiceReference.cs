/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Encapsulates a service and its dependencies into a reference counted container.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    public class ServiceReference : DisposeByRefObject
    {
        private readonly ServiceReferenceCollection FDependencies = new ServiceReferenceCollection();

        private readonly WriteOnce<object> FValue = new WriteOnce<object>(strict: false);

        /// <summary>
        /// Creates a new <see cref="ServiceReference"/> instance.
        /// </summary>
        public ServiceReference(AbstractServiceEntry entry, IInjector injector = null)
        {
            RelatedServiceEntry = Ensure.Parameter.IsNotNull(entry, nameof(entry));
            RelatedInjector = injector; // lehet null
        }

        /// <summary>
        /// The related service entry.
        /// </summary>
        public AbstractServiceEntry RelatedServiceEntry { get; }

        /// <summary>
        /// The (optional) <see cref="IInjector"/> instance who created this reference.
        /// </summary>
        public IInjector RelatedInjector { get; }

        /// <summary>
        /// The dependencies of the bound service. It can be used only if the <see cref="DisposeSuppressed"/> is false.
        /// </summary>
        public ICollection<ServiceReference> Dependencies 
        {
            get 
            {
                Ensure.NotDisposed(this);

                if (DisposeSuppressed)
                    throw new InvalidOperationException(); // TODO

                return FDependencies;
            }
        }

        /// <summary>
        /// The bound service instance.
        /// </summary>
        public object Value
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
                Ensure.Type.IsTypeOf(RelatedServiceEntry.Interface, value);
                Ensure.NotDisposed(this);

                FValue.Value = value;
            }
        }

        /// <summary>
        /// Returns true if the bound service will be disposed outside of this library false otherwise.
        /// </summary>
        public bool DisposeSuppressed { get; private set; }

        /// <summary>
        /// Marks the bound service to be disposed outside of this library.
        /// </summary>
        public void SuppressDispose() 
        {
            Ensure.NotDisposed(this);

            if (DisposeSuppressed) return;

            //
            // Ha mar vannak felvett fuggosegek akkor a metodus nem ertelmezett.
            //

            if (FDependencies.Any())
                throw new InvalidOperationException(); // TODO

            DisposeSuppressed = true;
        }

        /// <summary>
        /// Disposes the bound service then decrements the reference counter of all its dependencies.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                if (!DisposeSuppressed)
                {
                    Debug.WriteLine($"Disposing service: {RelatedServiceEntry}");

                    //
                    // Elso helyen szerepeljen h a fuggosegeket a Dispose()-ban meg hasznalni tudja a szerviz peldany.
                    //

                    (Value as IDisposable)?.Dispose();
                }

                //
                // Mivel ez a DisposeSuppressed erteketol fuggetlenul letre van hozva (max ures) ezert mindenkepp
                // felszabaditjuk.
                //

                FDependencies.Dispose();
            }

            base.Dispose(disposeManaged);
        }
#if !NETSTANDARD1_6
        /// <summary>
        /// Asynchronously disposes the bound service then decrements the reference counter of all its dependencies.
        /// </summary>
        protected async override ValueTask AsyncDispose()
        {
            if (!DisposeSuppressed)
            {
                Debug.WriteLine($"Disposing service: {RelatedServiceEntry}");

                if (Value is IAsyncDisposable disposable)
                    await disposable.DisposeAsync();
            }

            FDependencies.Dispose();

            //
            // Nem kell "base" hivas mert az a standard Dispose()-t hivna.
            //
        }
#endif
    }
}
