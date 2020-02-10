/********************************************************************************
* ServiceReferenceCollection.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceReferenceCollection : Disposable, ICollection<ServiceReference>
    {
        private readonly List<ServiceReference> FUnderlyingList;

        public ServiceReferenceCollection(int capacity = 10) => FUnderlyingList = new List<ServiceReference>(capacity);

        public int Count => FUnderlyingList.Count;

        public bool IsReadOnly => false;

        public void Add(ServiceReference item)
        {
            FUnderlyingList.Add(item);
            item.AddRef();
        }

        public void Clear()
        {
            FUnderlyingList.ForEach(@ref => @ref.Release());
            FUnderlyingList.Clear();
        }

        public bool Contains(ServiceReference item) => FUnderlyingList.Contains(item);

        public void CopyTo(ServiceReference[] array, int arrayIndex) => throw new NotSupportedException();

        public IEnumerator<ServiceReference> GetEnumerator() => FUnderlyingList.GetEnumerator();

        public bool Remove(ServiceReference item)
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
