/********************************************************************************
* ConcurrentCollection.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Primitives.Threading;

    internal class ConcurrentCollection<T> : ICollection<T>, IReadOnlyCollection<T>
    {
        private readonly ConcurrentLinkedList<T> FUnderlyingList = new();

        public int Count => FUnderlyingList.Count;

        public bool IsReadOnly { get; }

        public void Add(T item) => FUnderlyingList.Add(new LinkedListNode<T> { Value = item });

        public IEnumerator<T> GetEnumerator() => FUnderlyingList.Select(node => node.Value).GetEnumerator()!;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Clear() => throw new NotImplementedException();

        public bool Contains(T item) => throw new NotImplementedException();

        public void CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();

        public bool Remove(T item) => throw new NotImplementedException();
    }
}
