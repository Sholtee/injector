/********************************************************************************
* AbstractServiceReference.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Encapsulates a service and its dependencies into a reference counted container.
    /// </summary>
    public abstract class AbstractServiceReference : DisposeByRefObject
    {
        private object FInstance;

        /// <summary>
        /// Creates a new <see cref="AbstractServiceReference"/> instance.
        /// </summary>
        public AbstractServiceReference(AbstractServiceEntry entry) => RelatedServiceEntry = entry;

        /// <summary>
        /// The related service entry.
        /// </summary>
        public AbstractServiceEntry RelatedServiceEntry { get; }

        /// <summary>
        /// The referenced service instance.
        /// </summary>
        public object Instance 
        {
            get 
            {
                CheckDisposed();
                return FInstance;
            }
            set 
            {
                CheckDisposed();

                //
                // Peldany csak egyszer allithato be.
                //

                if (FInstance != null) 
                    throw new InvalidOperationException(); // TODO: hibauzenet

                FInstance = value;
            }
        }

        /// <summary>
        /// The dependencies of the service <see cref="Instance"/>.
        /// </summary>
        public abstract ICollection<AbstractServiceReference> Dependencies { get; }

        /// <summary>
        /// See <see cref="Disposable.Dispose(bool)"/>
        /// </summary>
        /// <param name="disposeManaged"></param>
        protected override void Dispose(bool disposeManaged)
        {
            Debug.WriteLineIf(disposeManaged, $"Disposed service: {RelatedServiceEntry.FriendlyName()}");
            base.Dispose(disposeManaged);
        }
    }
}
