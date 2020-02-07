/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceReference : AbstractServiceReference
    {
        private readonly ServiceCollection FDependencies = new ServiceCollection();

        public ServiceReference(AbstractServiceEntry entry, IInjector injector = null) : base(entry, injector) { }

        public override ICollection<AbstractServiceReference> Dependencies 
        {
            get 
            {
                CheckDisposed();
                return FDependencies;
            }
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Elso helyen szerepeljen h a fuggosegeket a Dispose()-ban meg hasznalni tudja a szerviz peldany.
                //

                (Value as IDisposable)?.Dispose();

                //
                // Ha a GC-nek kell felszabaditania a ServiceReference peldanyt akkor vmit nagyon elkurtunk
                // es mar semmi ertelme a referencia szamlalasnak -> ezert a lenti sor csak "disposeManaged"
                // eseten ertelmezett.
                //

                FDependencies.Dispose();
            }

            base.Dispose(disposeManaged);
        }
    }
}
