/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    internal sealed class ServiceReference : DisposeByRefObject, IServiceReference
    {
        private readonly List<IServiceReference> FDependencies = new List<IServiceReference>();

        private readonly WriteOnce FValue = new WriteOnce(strict: false);

        public ServiceReference(AbstractServiceEntry entry, IInjector injector)
        {
            RelatedServiceEntry = Ensure.Parameter.IsNotNull(entry, nameof(entry));
            RelatedInjector = Ensure.Parameter.IsNotNull(injector, nameof(injector));
        }

        public ServiceReference(AbstractServiceEntry entry, object value, bool externallyOwned)
        {
            RelatedServiceEntry = Ensure.Parameter.IsNotNull(entry, nameof(entry));
            Value = value; // set_Value() validal
            ExternallyOwned = externallyOwned;

            //
            // Elore definialt szerviz peldanynak nem lehet fuggosege.
            //
        }

        public AbstractServiceEntry RelatedServiceEntry { get; }

        public IInjector? RelatedInjector { get; }

        public IReadOnlyCollection<IServiceReference> Dependencies 
        {
            get 
            {
                CheckNotDisposed();
                return FDependencies;
            }
        }

        public void AddDependency(IServiceReference dependency) 
        {
            CheckNotDisposed();

            Ensure.Parameter.IsNotNull(dependency, nameof(dependency));

            //
            // Ha mar letre lett hozva a szervizpeldany akkor utolag nem vehetunk fel fuggoseget hozza.
            //

            if (Value is not null)
                throw new InvalidOperationException();

            dependency.AddRef();

            try
            {
                FDependencies.Add(dependency);
            }
            catch 
            {
                dependency.Release();
            }
        }

        public object? Value
        {
            get
            {
                CheckNotDisposed();
                return FValue.Value;
            }
            set 
            {
                CheckNotDisposed();

                Ensure.Parameter.IsNotNull(value, nameof(value));

                FValue.Value = value;
            }
        }

        public bool ExternallyOwned { get; }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged && !ExternallyOwned)
            {
                //
                // Elso helyen szerepeljen h a fuggosegeket a Dispose()-ban meg hasznalni tudja a szerviz peldany.
                //

                if (Value is IDisposable disposable)
                    disposable.Dispose();

                foreach (IServiceReference dep in Dependencies)
                    dep.Release();          
            }

            base.Dispose(disposeManaged);
        }

        protected async override ValueTask AsyncDispose()
        {
            if (!ExternallyOwned)
            {
                switch (Value)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync();
                        break;

                    //
                    // Ha aszinkron nem lehet megprobaljuk szinkron is felszabaditani.
                    //

                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }

                await Task.WhenAll
                (
                    Dependencies.Select(dep => dep.ReleaseAsync())
                );
            }

            //
            // Nem kell "base" hivas mert az a standard Dispose()-t hivna.
            //
        }
    }
}
