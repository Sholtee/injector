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
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    public abstract class AbstractServiceReference : DisposeByRefObject
    {
        private object FValue;

        /// <summary>
        /// Creates a new <see cref="AbstractServiceReference"/> instance.
        /// </summary>
        public AbstractServiceReference(AbstractServiceEntry entry, IInjector injector)
        {
            RelatedServiceEntry = entry ?? throw new ArgumentNullException(nameof(entry));
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
        /// The referenced service instance.
        /// </summary>
        public object Value 
        {
            get 
            {
                CheckDisposed();
                return FValue;
            }
            set 
            {
                CheckDisposed();

                //
                // Peldany csak egyszer allithato be.
                //

                if (FValue != null) 
                    throw new InvalidOperationException(); // TODO: hibauzenet

                FValue = value;
            }
        }

        /// <summary>
        /// The dependencies of the service <see cref="Value"/>.
        /// </summary>
        public abstract ICollection<AbstractServiceReference> Dependencies { get; }

        /// <summary>
        /// Sets the <see cref="Value"/> of this instance.
        /// </summary>
        public virtual bool SetInstance() => RelatedServiceEntry.SetInstance(this);

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
