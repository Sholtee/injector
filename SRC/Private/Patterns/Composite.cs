/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// A thread safe <see cref="IComposite{T}"/> implementation.
    /// </summary>
    /// <typeparam name="TInterface">The interface on which we want to apply the composite pattern.</typeparam>
    /// <remarks>This is an internal class so it can be changed from version to version. Don't use it!</remarks>
    public abstract class Composite<TInterface>: Disposable, IComposite<TInterface> where TInterface: class, IComposite<TInterface>
    {
        #region Private
        private /*readonly*/ DictionaryWrap FChildren = new DictionaryWrap();
        private /*readonly*/ Composite<TInterface> FParent;

        private sealed class DictionaryWrap : IReadOnlyCollection<TInterface>
        {
            private readonly IDictionary<TInterface, byte> FEntries = new ConcurrentDictionary<TInterface, byte>();

            private ICollection<TInterface> Entries => FEntries.Keys;

            public void Add(TInterface item) => FEntries.Add(item, 0);

            public bool Remove(TInterface item) => FEntries.Remove(item);

            IEnumerator<TInterface> IEnumerable<TInterface>.GetEnumerator() => Entries.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();

            int IReadOnlyCollection<TInterface>.Count => FEntries.Count;
        }
        #endregion

        #region Protected
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="parent">The parent entity. It can be null.</param>
        protected Composite(Composite<TInterface> parent) => FParent = parent;

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

                if (FParent != null)
                {
                    bool removed = FParent.FChildren.Remove(Self);
                    Debug.Assert(removed, "Parent does not contain this instance");
                    FParent = null;
                }

                //
                // Osszes gyereket eltavolitjuk majd toroljuk a listat.
                //

                foreach (TInterface child in FChildren)
                {
                    child.Dispose();     
                }

                Debug.Assert(!FChildren.Any());
                FChildren = null;
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Access this entity as a <typeparam name="TInterface"/> interface.
        /// </summary>
        protected abstract TInterface Self { get; }

        /// <summary>
        /// Creates a new child. For more information see the <see cref="IComposite{T}"/> interface.
        /// </summary>
        /// <returns>The newly created child.</returns>
        protected abstract TInterface CreateChild();
        #endregion

        #region IComposite
        TInterface IComposite<TInterface>.Parent => FParent?.Self;

        IReadOnlyCollection<TInterface> IComposite<TInterface>.Children => FChildren;

        TInterface IComposite<TInterface>.CreateChild()
        {
            //
            // Figyelem: A legyartott entitas lehet proxy (tehat nem lesz Composite<TInterface> leszarmazott).
            //

            TInterface result = CreateChild();
            FChildren.Add(result);
            return result;
        }

        bool IComposite<TInterface>.IsRoot => FParent == null;
        #endregion
    }
}
