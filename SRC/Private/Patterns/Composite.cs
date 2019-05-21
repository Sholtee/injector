/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI
{
    public abstract class Composite<T>: Disposable, IComposite<T> where T: Composite<T>
    {
        #region Private
        private /*readonly*/ IDictionary<T, byte> FChildren = new ConcurrentDictionary<T, byte>();
        private /*readonly*/ T FParent;
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
                    FParent.FChildren.Remove((T) this);
                    FParent = null;
                }

                //
                // Osszes gyereket eltavolitjuk majd toroljuk a listat.
                //

                foreach (IDisposable child in FChildren.Keys)
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

        IReadOnlyList<T> IComposite<T>.Children => FChildren.Keys.ToArray();

        T IComposite<T>.CreateChild()
        {
            T result = CreateChild();
            FChildren.Add(result, 0);
            return result;
        }
        #endregion
    }
}
