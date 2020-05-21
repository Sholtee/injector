/********************************************************************************
* ServiceReferenceCollection.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed class ServiceReferenceCollection : Disposable, ICollection<IServiceReference>
    {
        private readonly List<IServiceReference> FUnderlyingList;

        public ServiceReferenceCollection(int capacity = 0) => FUnderlyingList = new List<IServiceReference>(capacity);

        public int Count => FUnderlyingList.Count;

        public bool IsReadOnly => false;

        public void Add(IServiceReference item)
        {
            FUnderlyingList.Add(item);
            item.AddRef();
        }

        public void Clear()
        {
            FUnderlyingList.ForEach(@ref => @ref.Release());
            FUnderlyingList.Clear();
        }

        public bool Contains(IServiceReference item) => FUnderlyingList.Contains(item);

        public void CopyTo(IServiceReference[] array, int arrayIndex) => throw new NotSupportedException();

        public IEnumerator<IServiceReference> GetEnumerator() => FUnderlyingList.GetEnumerator();

        public bool Remove(IServiceReference item)
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

        protected async override ValueTask AsyncDispose()
        {
            foreach (ServiceReference @ref in FUnderlyingList)
                await @ref.ReleaseAsync();

            FUnderlyingList.Clear();

            //
            // Nem kell "base" hivas mert az a standard Dispose()-t hivna.
            //
        }
    }
}
