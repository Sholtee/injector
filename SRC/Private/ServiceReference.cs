﻿/********************************************************************************
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
    public class ServiceReference : DisposeByRefObject
    {
        private readonly ServiceCollection FDependencies = new ServiceCollection();

        /// <summary>
        /// Creates a new <see cref="ServiceReference"/> instance.
        /// </summary>
        public ServiceReference(AbstractServiceEntry entry) => RelatedServiceEntry = entry;

        /// <summary>
        /// The related service entry.
        /// </summary>
        public AbstractServiceEntry RelatedServiceEntry { get; }

        /// <summary>
        /// The referenced service instance.
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// The dependencies of this service <see cref="Instance"/>.
        /// </summary>
        public ICollection<ServiceReference> Dependencies => FDependencies;

        /// <summary>
        /// Disposes the referenced service <see cref="Instance"/> and decrements the reference counter of all the <see cref="Dependencies"/>.
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
