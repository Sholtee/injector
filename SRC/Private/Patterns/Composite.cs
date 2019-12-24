﻿/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Implements the <see cref="IComposite{T}"/> interface.
    /// </summary>
    /// <typeparam name="TInterface">The interface on which we want to apply the composite pattern.</typeparam>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    public abstract class Composite<TInterface>: Disposable, IComposite<TInterface> where TInterface: class, IComposite<TInterface>
    {
        private readonly HashSet<TInterface> FChildren = new HashSet<TInterface>();
        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim();

        private TInterface Self => this as TInterface;

        /// <summary>
        /// The maxmimum number of children this entity can hold.
        /// </summary>
        internal int MaxChildCount { get; set; } = Config.Value.CompositeMaxChildCount; // tesztekhez

        #region Protected
        /// <summary>
        /// Creates a new <see cref="Composite{TInterface}"/> instance.
        /// </summary>
        /// <param name="parent">The parent entity. It can be null.</param>
        protected Composite(TInterface parent) => parent?.AddChild(Self);

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

                Parent?.RemoveChild(Self);

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
        /// The parent of this entity. Can be null.
        /// </summary>
        public TInterface Parent { get; set; }

        /// <summary>
        /// The children of this entity.
        /// </summary>
        public virtual IReadOnlyCollection<TInterface> Children
        {
            get
            {
                CheckDisposed();

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
        /// Adds a new child to the <see cref="Children"/> list. For more information see the <see cref="IComposite{T}"/> interface.
        /// </summary>
        public virtual void AddChild(TInterface child)
        {
            CheckDisposed();

            if (child == null)
                throw new ArgumentNullException(nameof(child));

            if (child.Parent != null)
                throw new InvalidOperationException(Resources.NOT_ORPHAN);

            using (FLock.AcquireWriterLock())
            {
                if (FChildren.Count == MaxChildCount)
                    //
                    // Ha ide eljutunk az pl azt jelentheti h egy kontenerbol letrehozott injectorok 
                    // nincsenek a munkafolyamat vegen felszbaditva (a GC nem tudja felszabaditani 
                    // oket mivel a szulo kontener gyermekei).
                    //

                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.TOO_MANY_CHILDREN, MaxChildCount));

                FChildren.Add(child);
            }

            child.Parent = Self;
        }

        /// <summary>
        /// Removes a child from the <see cref="Children"/> list. For more information see the <see cref="IComposite{T}"/> interface.
        /// </summary>
        public virtual void RemoveChild(TInterface child)
        {
            CheckDisposed();

            if (child == null)
                throw new ArgumentNullException(nameof(child));

            if (child.Parent != this)
                throw new InvalidOperationException(Resources.INVALID_PARENT);

            using (FLock.AcquireWriterLock())
                FChildren.Remove(child);

            child.Parent = null;
        }
        #endregion
    }

    public partial class Config 
    {
        /// <summary>
        /// Limits the count of children belong to a <see cref="Composite{TInterface}"/> entity.
        /// </summary>
        public int CompositeMaxChildCount { get; set; } = 512;
    }
}
