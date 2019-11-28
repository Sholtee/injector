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
    public class ServiceReference: DisposeByRefObject
    {
        /// <summary>
        /// Creates a new <see cref="ServiceReference"/> instance.
        /// </summary>
        /// <param name="instance">The instance to make referenced.</param>
        /// <param name="dependencies">The dependencies of the instance.</param>
        public ServiceReference(object instance, IReadOnlyCollection<ServiceReference> dependencies) 
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));

            foreach (ServiceReference dependency in dependencies)
                dependency.AddRef();
        }

        /// <summary>
        /// The referenced service instance.
        /// </summary>
        public object Instance { get; }

        /// <summary>
        /// The dependencies of this service <see cref="Instance"/>.
        /// </summary>
        public IReadOnlyCollection<ServiceReference> Dependencies { get; }

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

                foreach (ServiceReference dependency in Dependencies)
                    dependency.Release();
            }

            base.Dispose(disposeManaged);
        }
    }
}
