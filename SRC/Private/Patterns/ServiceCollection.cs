/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceCollection : Disposable, ICollection<AbstractServiceReference>
    {
        private readonly List<AbstractServiceReference> FUnderlyingList;

        public ServiceCollection(int capacity = 10) => FUnderlyingList = new List<AbstractServiceReference>(capacity);

        public int Count => FUnderlyingList.Count;

        public bool IsReadOnly => false;

        public void Add(AbstractServiceReference item)
        {
            FUnderlyingList.Add(item);
            item.AddRef();
        }

        public void Clear()
        {
            FUnderlyingList.ForEach(@ref => @ref.Release());
            FUnderlyingList.Clear();
        }

        public bool Contains(AbstractServiceReference item) => FUnderlyingList.Contains(item);

        public void CopyTo(AbstractServiceReference[] array, int arrayIndex) => throw new NotSupportedException();

        public IEnumerator<AbstractServiceReference> GetEnumerator() => FUnderlyingList.GetEnumerator();

        public bool Remove(AbstractServiceReference item)
        {
            if (FUnderlyingList.Remove(item)) 
            {
                item.Release();
                return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) Clear();

            base.Dispose(disposeManaged);
        }
    }
}
