/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// An <see cref="IComposite{T}"/> implementation.
    /// </summary>
    /// <typeparam name="TInterface">The interface on which we want to apply the composite pattern.</typeparam>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    public abstract class Composite<TInterface>: Disposable, IComposite<TInterface> where TInterface: class, IComposite<TInterface>
    {
        private readonly HashSet<TInterface> FChildren = new HashSet<TInterface>();
        private readonly IComposite<TInterface> FParent;
        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim();

        #region Protected
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="parent">The parent entity. It can be null.</param>
        protected Composite(IComposite<TInterface> parent)
        {
            FParent = parent;
            FParent?.AddChild(this as TInterface);
        }

        /// <summary>
        /// Disposal logic related to this class.
        /// </summary>
        /// <param name="disposeManaged">Check out the <see cref="Disposable"/> class.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Kivesszuk magunkat a szulo gyerekei kozul (kiveve ha gyoker elemunk van, ott nincs szulo).
                //

                FParent?.RemoveChild(this as TInterface);

                //
                // Osszes gyereket Dispose()-oljuk. A ToList()-es varazslat azert kell h iteracio kozben
                // is kivehessunk elemet a listabol.
                //

                FChildren.ToList().ForEach(child => child.Dispose());
                Debug.Assert(!FChildren.Any());

                FLock.Dispose();
            }

            base.Dispose(disposeManaged);
        }
        #endregion

        #region IComposite
        public TInterface Parent => FParent as TInterface;

        public virtual IReadOnlyCollection<TInterface> Children
        {
            get
            {
                using (FLock.AcquireReaderLock())
                {
                    //
                    // Masolatot adjunk vissza.
                    //

                    return FChildren.ToArray();
                }
            }
        }

        /// <summary>
        /// Creates a new child. For more information see the <see cref="IComposite{T}"/> interface.
        /// </summary>
        /// <returns>The newly created child.</returns>
        public abstract TInterface CreateChild();

        public virtual void AddChild(TInterface child)
        {
            Debug.Assert(child.Parent == this, "Invalid parent");  // TODO: exception

            using (FLock.AcquireWriterLock())
            {
                Debug.Assert(!FChildren.Contains(child), "Attempt to add a child twice");

                FChildren.Add(child);
            }
        }

        public virtual void RemoveChild(TInterface child)
        {
            bool removed;

            using (FLock.AcquireWriterLock())
                removed = FChildren.Remove(child);
                    
            Debug.Assert(removed, "Child could not be found"); // TODO: exception  
        }
        #endregion
    }
}
