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
    public abstract class Composite<T>: Disposable, IComposite<T> where T: class, IComposite<T>
    {
        #region Private
        private /*readonly*/ ICollection<T> FChildren = new DictionaryWrap(new ConcurrentDictionary<T, byte>());
        private /*readonly*/ T FParent;

        private sealed class DictionaryWrap : ICollection<T>
        {
            private readonly IDictionary<T, byte> FEntries;

            private ICollection<T> Entries => FEntries.Keys;

            public DictionaryWrap(IDictionary<T, byte> entries) { FEntries = entries; }

            IEnumerator<T> IEnumerable<T>.GetEnumerator() { return Entries.GetEnumerator(); }

            IEnumerator IEnumerable.GetEnumerator() { return Entries.GetEnumerator(); }

            void ICollection<T>.Add(T item){ FEntries.Add(item, 0); }

            void ICollection<T>.Clear() { FEntries.Clear(); }

            bool ICollection<T>.Contains(T item){ return Entries.Contains(item); }

            void ICollection<T>.CopyTo(T[] array, int arrayIndex) { throw new NotImplementedException(); }

            bool ICollection<T>.Remove(T item) { return FEntries.Remove(item); }

            int ICollection<T>.Count => FEntries.Count;

            bool ICollection<T>.IsReadOnly => FEntries.IsReadOnly;
        }
        #endregion

        #region Protected
        protected Composite(T parent)
        {
            FParent = parent;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Kivesszuk magunkat a szulo gyerekei kozul (kiveve ha gyoker elemunk van,
                // ott nincs szulo).
                //

                if (FParent != null)
                {
                    bool removed = FParent.Children.Remove(this as T);
#if DEBUG
                    Debug.Assert(removed, "Parent does not contain this instance");
#endif
                    FParent = null;
                }

                //
                // Osszes gyereket eltavolitjuk majd toroljuk a listat.
                //

                foreach (T child in FChildren)
                {
                    child.Dispose();     
                }

                FChildren.Clear();
                FChildren = null;
            }

            base.Dispose(disposeManaged);
        }

        protected abstract T CreateChild();
        #endregion

        #region IComposite
        T IComposite<T>.Parent => FParent;

        ICollection<T> IComposite<T>.Children => FChildren;

        T IComposite<T>.CreateChild()
        {
            T result = CreateChild();
            FChildren.Add(result);
            return result;
        }

        bool IComposite<T>.IsRoot => FParent == null;
        #endregion
    }
}
