/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Properties;

    /// <summary>
    /// Implements the <see cref="IServiceCollection"/> interface.
    /// </summary>
    public sealed class ServiceCollection : IServiceCollection
    {
        private readonly Dictionary<IServiceId, AbstractServiceEntry> FEntries = new(IServiceId.Comparer.Instance);

        private void CheckNotReadOnly()
        {
            if (IsReadOnly)
                throw new InvalidOperationException(Resources.COLLECTION_IS_READONLY);
        }

        /// <summary>
        /// Number of entries in this list.
        /// </summary>
        public int Count => FEntries.Count;

        /// <summary>
        /// This list is
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// Returns true if service overriding is supported.
        /// </summary>
        public bool SupportsOverride { get; }

        /// <summary>
        /// Adds a new entry to this collection.
        /// </summary>
        /// <exception cref="ServiceAlreadyRegisteredException">If <see cref="SupportsOverride"/> is false and this list already contains an entry with the given id</exception>
        public void Add(AbstractServiceEntry item)
        {
            CheckNotReadOnly();

            if (item is null)
                throw new ArgumentNullException(nameof(item));

            if (!SupportsOverride && FEntries.ContainsKey(item))
                throw new ServiceAlreadyRegisteredException(Resources.SERVICE_ALREADY_REGISTERED, item);

            FEntries[item] = item;
        }

        /// <summary>
        /// Check entry existance by id.
        /// </summary>
        public bool Contains(IServiceId id) => FEntries.ContainsKey(id ?? throw new ArgumentNullException(nameof(id)));

        /// <summary>
        /// Drops entry by id.
        /// </summary>
        public bool Remove(IServiceId id)
        {
            CheckNotReadOnly();
            return FEntries.Remove(id ?? throw new ArgumentNullException(nameof(id)));
        }

        /// <summary>
        /// Tries to find an entry by id.
        /// </summary>
        public AbstractServiceEntry? TryFind(IServiceId id)
        {
            FEntries.TryGetValue(id ?? throw new ArgumentNullException(nameof(id)), out AbstractServiceEntry? result);
            return result;
        }

        /// <summary>
        /// Clears this list.
        /// </summary>
        public void Clear()
        {
            CheckNotReadOnly();
            FEntries.Clear();
        }

        /// <summary>
        /// Check entry existance by reference.
        /// </summary>
        public bool Contains(AbstractServiceEntry item) =>
            FEntries.TryGetValue(item ?? throw new ArgumentNullException(nameof(item)), out AbstractServiceEntry stored) && 
            item == stored;

        /// <summary>
        /// Copies the contained entries to an array.
        /// </summary>
        public void CopyTo(AbstractServiceEntry[] array, int arrayIndex) => FEntries.Values.CopyTo
        (
            array ?? throw new ArgumentNullException(nameof(array)),
            arrayIndex
        );

        /// <summary>
        /// Drops entry by reference.
        /// </summary>
        public bool Remove(AbstractServiceEntry item)
        {
            CheckNotReadOnly();

            if (Contains(item))
            {
                FEntries.Remove(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enumerates the entries in this collection.
        /// </summary>
        public IEnumerator<AbstractServiceEntry> GetEnumerator() => FEntries.Values.GetEnumerator();

        /// <summary>
        /// Makes this collection read only.
        /// </summary>
        public void MakeReadOnly() => IsReadOnly = true;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Creates a new <see cref="ServiceCollection"/> instance.
        /// </summary>
        public ServiceCollection(bool supportsOverride = false) => SupportsOverride = supportsOverride;
    }
}