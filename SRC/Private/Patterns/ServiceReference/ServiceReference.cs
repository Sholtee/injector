/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Encapsulates a service and its dependencies into a reference counted container.
    /// </summary>
    public class ServiceReference : AbstractServiceReference
    {
        private readonly ServiceCollection FDependencies = new ServiceCollection();

        /// <summary>
        /// Creates a new <see cref="ServiceReference"/> instance.
        /// </summary>
        public ServiceReference(AbstractServiceEntry entry) : base(entry) { }

        /// <summary>
        /// See <see cref="AbstractServiceReference.Dependencies"/>.
        /// </summary>
        public override ICollection<AbstractServiceReference> Dependencies 
        {
            get 
            {
                CheckDisposed();
                return FDependencies;
            }
        }

        /// <summary>
        /// Disposes the referenced service <see cref="AbstractServiceReference.Instance"/> and decrements the reference counter of all the <see cref="Dependencies"/>.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
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
