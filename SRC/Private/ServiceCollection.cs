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

        protected virtual ServiceEntry QueryInternal(Type iface)
        {
            if (Query(iface, out var entry)) return entry;

            //
            // Meg benne lehet generikus formaban.
            //

            if (!iface.IsGenericType() || !Query(iface.GetGenericTypeDefinition(), out entry))
                throw new ServiceNotFoundException(iface);

            return entry;

            bool Query(Type key, out ServiceEntry val) => FEntries.TryGetValue(key, out val);
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
        public ServiceCollection(IReadOnlyCollection<ServiceEntry> inheritedEntries)
        {
            FEntries = new Dictionary<Type, ServiceEntry>(inheritedEntries?.Count ?? 0);
            if (inheritedEntries == null) return;

            foreach (ServiceEntry entry in inheritedEntries)
            {
                entry.CopyTo(this);
            }
        }
        
        /// <summary>
        /// Gets the entry associated with the given interface.
        /// </summary>
        /// <param name="iface">The "id" of the entry. Must be an interface <see cref="Type"/>.</param>
        /// <returns>The stored <see cref="ServiceEntry"/> instance.</returns>
        /// <remarks>This method supports entry specialization which means after registering a generic entry you can query its (unregistered) closed pair by passing the closed interface <see cref="Type"/> to this function.</remarks>
        public virtual ServiceEntry Query(Type iface)
        {
            ServiceEntry entry = QueryInternal(iface);

            //
            // 1. eset: Azt az entitast adjuk vissza amit kerestunk.
            //

            if (entry.Interface == iface) return entry;

            //
            // 2. eset: Egy generikus bejegyzes lezart parjat kerdezzuk le
            //

            if (entry.IsGeneric()) return entry.Factory != null 
                //
                // 2a eset: A bejegyzesnek van beallitott gyar fv-e (pl Factory<TCica>() hivas).
                //

                ? entry

                //
                // 2b eset: Konkretizalni kell a tipust. Megjegyzendo h Specialize() rogiziti is az uj bejegyzest
                //          -> legkozelebbi hivasnal mar az 1. eset fog lefutni.
                //

                : entry.Specialize(iface.GetGenericArguments());

            throw new InvalidOperationException();           
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
            foreach (IDisposable disposable in FEntries.Values.Where(entry => entry.Owner == this))
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