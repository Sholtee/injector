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
    public class ServiceReference : DisposeByRefObject, IServiceID
    {
        /// <summary>
        /// Creates a new <see cref="ServiceReference"/> instance.
        /// </summary>
        public ServiceReference(Type iface, string name) 
        {
            Interface = iface;
            Name = name;
        }

        /// <summary>
        /// The interface of the service.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The (optional) name of the service.
        /// </summary>
        public string Name { get;  }

        /// <summary>
        /// The referenced service instance.
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// The dependencies of this service <see cref="Instance"/>.
        /// </summary>
        public ICollection<ServiceReference> Dependencies { get; } = new ServiceCollection();

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

                Dependencies.Clear();
            }

            base.Dispose(disposeManaged);
        }
    }
}
