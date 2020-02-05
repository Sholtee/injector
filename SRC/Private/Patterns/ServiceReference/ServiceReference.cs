/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceReference : AbstractServiceReference
    {
        private readonly ServiceCollection FDependencies = new ServiceCollection();

        public ServiceReference(AbstractServiceEntry entry) : base(entry) { }

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
            Debug.WriteLine($"Disposed service: {RelatedServiceEntry.Interface.Name}");

            if (disposeManaged)
            {
                //
                // Elso helyen szerepeljen h a fuggosegeket a Dispose()-ban meg hasznalni tudja a szerviz peldany.
                //

                (Instance as IDisposable)?.Dispose();

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
