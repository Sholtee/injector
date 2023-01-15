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
    using Interfaces;
    using Properties;

    internal sealed class ServiceCollection : IModifiedServiceCollection, IReadOnlyCollection<AbstractServiceEntry>
    {
        private readonly HashSet<AbstractServiceEntry> FUnderlyingCollection = new(ServiceIdComparer<AbstractServiceEntry>.Instance);
        private AbstractServiceEntry? FLastEntry;

        public void Add(AbstractServiceEntry item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            if (!FUnderlyingCollection.Add(item ?? throw new ArgumentNullException(nameof(item))))
                throw new ServiceAlreadyRegisteredException(Resources.SERVICE_ALREADY_REGISTERED);
  
            FLastEntry = item;
        }

        public void Clear() => FUnderlyingCollection.Clear();

        public bool Contains(AbstractServiceEntry item) => FUnderlyingCollection.Contains(item ?? throw new ArgumentNullException(nameof(item)));

        public void CopyTo(AbstractServiceEntry[] array, int arrayIndex) => FUnderlyingCollection.CopyTo(array ?? throw new ArgumentNullException(nameof(array)), arrayIndex);

        public bool Remove(AbstractServiceEntry item) => FUnderlyingCollection.Remove(item ?? throw new ArgumentNullException(nameof(item)));

        public IEnumerator<AbstractServiceEntry> GetEnumerator() => FUnderlyingCollection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public ServiceCollection(ServiceOptions? serviceOptions = null) : base()
            => ServiceOptions = serviceOptions ?? ServiceOptions.Default;

        public ServiceCollection(IServiceCollection src) : this(src.ServiceOptions)
        {
            foreach (AbstractServiceEntry entry in src)
            {
                Add(entry);
            }
        }

        public AbstractServiceEntry LastEntry => FLastEntry ?? throw new InvalidOperationException();

        public ServiceOptions ServiceOptions { get; }

        public int Count => FUnderlyingCollection.Count;

        public bool IsReadOnly { get; }
    }
}