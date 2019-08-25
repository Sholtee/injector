/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Implements the mechanism of storing service entries.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    internal class ServiceCollection : Disposable, ICollection<ServiceEntry>, IReadOnlyCollection<ServiceEntry>
    {
        protected readonly Dictionary<Type, ServiceEntry> FEntries;

        #region Protected
        /// <summary>
        /// See <see cref="Disposable"/>.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                Clear();
            }

            base.Dispose(disposeManaged);
        }
        #endregion

        #region Public
        /// <summary>
        /// Creates a new <see cref="ServiceCollection"/> instance.
        /// </summary>
        public ServiceCollection() : this(null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ServiceCollection"/> instance.
        /// </summary>
        /// <param name="inheritedEntries">Entries to be inherited.</param>
        public ServiceCollection(IEnumerable<ServiceEntry> inheritedEntries) => FEntries = new Dictionary<Type, ServiceEntry>
        (
            inheritedEntries?
                .ToDictionary
                (
                    entry => entry.Interface,
                    entry => (ServiceEntry) entry.Clone()
                )
            ?? new Dictionary<Type, ServiceEntry>(0)
        );
        
        /// <summary>
        /// Gets the entry associated with the given interface.
        /// </summary>
        /// <param name="iface">The "id" of the entry. Must be an interface <see cref="Type"/>.</param>
        /// <returns>The stored <see cref="ServiceEntry"/> instance.</returns>
        /// <remarks>This method supports entry specialization which means after registering a generic entry you can query its (unregistered) closed pair by passing the closed interface <see cref="Type"/> to this function.</remarks>
        public virtual ServiceEntry Query(Type iface)
        {
            if (QueryEntry(iface, out var entry)) return entry;

            //
            // Meg benne lehet generikus formaban.
            //

            if (!iface.IsGenericType() || !QueryEntry(iface.GetGenericTypeDefinition(), out var genericEntry))
                throw new ServiceNotFoundException(iface);

            //
            // Ha a generikus bejegyzes legyarthato (van kivulrol beallitott Factory fv-e) akkor nincs dolgunk.
            //

            if (genericEntry.Factory != null) return genericEntry;

            //
            // Kuloben letrehozzuk a tipizalt bejegyzest, rogzitjuk, majd visszaadjuk azt.
            //

            Add(entry = genericEntry.Specialize(iface.GetGenericArguments()));
            return entry;
            
            bool QueryEntry(Type key, out ServiceEntry val) => FEntries.TryGetValue(key, out val);
        }
        #endregion

        #region ICollection
        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public virtual IEnumerator<ServiceEntry> GetEnumerator() => FEntries.Values.GetEnumerator();

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Adds an entry to the collection.
        /// </summary>
        /// <param name="item">The <see cref="ServiceEntry"/> instance to be added.</param>
        /// <remarks>A <see cref="ServiceAlreadyRegisteredException"/> is thrown if an item with the same interface already exists in the collection.</remarks>
        public virtual void Add(ServiceEntry item)
        {
            try
            {
                FEntries.Add(item.Interface, item);
            }
            catch (ArgumentException e)
            {
                throw new ServiceAlreadyRegisteredException(item.Interface, e);
            }
        }

        /// <summary>
        /// Clears the collection.
        /// </summary>
        /// <remarks>All the contained entries will be disposed.</remarks>
        public virtual void Clear()
        {
            foreach (IDisposable disposable in FEntries.Values)
                try
                {
                    disposable.Dispose();
                }
                #pragma warning disable CS0168 // Variable is declared but never used
                catch (Exception e)
                #pragma warning restore CS0168
                {
                    if (Debugger.IsAttached) Debugger.Break();
                }

            FEntries.Clear();
        }

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public virtual bool Contains(ServiceEntry item) => FEntries.ContainsValue(item);

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public virtual void CopyTo(ServiceEntry[] array, int arrayIndex)
        {
            int i = 0; 
            foreach (ServiceEntry entry in this)
            {
                array.SetValue(entry, i++ + arrayIndex);
            }
        }

        /// <summary>
        /// NOT SUPPORTED! Don't use!
        /// </summary>
        public virtual bool Remove(ServiceEntry item) => throw new NotSupportedException();

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public virtual int Count => FEntries.Count;

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public bool IsReadOnly => false;
        #endregion
    }
}