/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Properties;

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

                Parent?.RemoveChild(this as TInterface);

                //
                // Osszes gyereket Dispose()-oljuk. Mivel a Children amugy is masolatot ad vissza ezert
                // iteracio kozben is kivehessunk elemet a listabol.
                //

                foreach (TInterface child in Children)
                {
                    child.Dispose();            
                }
                
                Debug.Assert(!Children.Any());

                FLock.Dispose();
            }

            base.Dispose(disposeManaged);
        }
        #endregion

        #region IComposite
        /// <summary>
        /// See <see cref="IComposite{T}"/>
        /// </summary>
        public TInterface Parent => FParent as TInterface;

        /// <summary>
        /// See <see cref="IComposite{T}"/>
        /// </summary>
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

        /// <summary>
        /// See <see cref="IComposite{T}"/>
        /// </summary>
        public virtual void AddChild(TInterface child)
        {
            if (child.Parent != this)
                throw new InvalidOperationException(Resources.INVALID_PARENT);

            using (FLock.AcquireWriterLock())
                if (!FChildren.Add(child))
                    throw new InvalidOperationException(Resources.CHILD_ALREADY_CONTAINED);
        }

        /// <summary>
        /// See <see cref="IComposite{T}"/>
        /// </summary>
        public virtual void RemoveChild(TInterface child)
        {
            if (child.Parent != this)
                throw new InvalidOperationException(Resources.INVALID_PARENT);

            using (FLock.AcquireWriterLock())
                if (!FChildren.Remove(child))
                    throw new InvalidOperationException(Resources.CHILD_NOT_CONTAINED);
        }
        #endregion
    }
}
