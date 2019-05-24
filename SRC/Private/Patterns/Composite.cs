/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    public abstract class Composite<TInterface>: Disposable, IComposite<TInterface> where TInterface: class, IComposite<TInterface>
    {
        #region Private
        private /*readonly*/ ICollection<TInterface> FChildren = new DictionaryWrap(new ConcurrentDictionary<TInterface, byte>());
        private /*readonly*/ TInterface FParent;

        private sealed class DictionaryWrap : ICollection<TInterface>
        {
            private readonly IDictionary<TInterface, byte> FEntries;

            private ICollection<TInterface> Entries => FEntries.Keys;

            public DictionaryWrap(IDictionary<TInterface, byte> entries) => FEntries = entries;

            IEnumerator<TInterface> IEnumerable<TInterface>.GetEnumerator() => Entries.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => Entries.GetEnumerator();

            void ICollection<TInterface>.Add(TInterface item) => FEntries.Add(item, 0);

            void ICollection<TInterface>.Clear() => FEntries.Clear();

            bool ICollection<TInterface>.Contains(TInterface item) => Entries.Contains(item);

            void ICollection<TInterface>.CopyTo(TInterface[] array, int arrayIndex) => throw new NotImplementedException();

            bool ICollection<TInterface>.Remove(TInterface item) => FEntries.Remove(item);

            int ICollection<TInterface>.Count => FEntries.Count;

            bool ICollection<TInterface>.IsReadOnly => FEntries.IsReadOnly;
        }
        #endregion

        #region Protected
        protected Composite(TInterface parent)
        {
            FParent = parent;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Kivesszuk magunkat a szulo gyerekei kozul (kiveve ha gyoker elemunk van, ott nincs szulo).
                //

                if (FParent != null)
                {
                    bool removed = FParent.Children.Remove(Self);
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

                FChildren.Clear();
                FChildren = null;
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Korbedolgozas h TInterface lehessen proxy.
        /// </summary>
        protected virtual TInterface Self => this as TInterface;

        protected abstract TInterface CreateChild();
        #endregion

        #region IComposite
        TInterface IComposite<TInterface>.Parent => FParent;

        ICollection<TInterface> IComposite<TInterface>.Children => FChildren;

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
