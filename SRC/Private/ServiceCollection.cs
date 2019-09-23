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
    internal class ServiceCollection : Disposable, IServiceCollection, IReadOnlyCollection<AbstractServiceEntry>
    {
        private readonly Dictionary<Type, AbstractServiceEntry> FEntries;

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

        protected bool ContainsByRef(AbstractServiceEntry item) => item != null && ReferenceEquals(Get(item.Interface)/*visszaadhat NULL-t*/, item);
        #endregion
  
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
        public ServiceCollection(IReadOnlyCollection<AbstractServiceEntry> inheritedEntries)
        {
            FEntries = new Dictionary<Type, AbstractServiceEntry>(inheritedEntries?.Count ?? 0);
            if (inheritedEntries == null) return;

            foreach (AbstractServiceEntry entry in inheritedEntries)
            {
                entry.CopyTo(this);
            }
        }

        #region IServiceCollection
        public virtual bool TryGet(Type iface, out AbstractServiceEntry entry) => FEntries.TryGetValue(iface, out entry);

        public virtual AbstractServiceEntry Get(Type iface) => TryGet(iface, out var result)
            ? result
            : throw new ServiceNotFoundException(iface);

        public virtual bool TryGetClosest(Type iface, out AbstractServiceEntry entry) =>
            TryGet(iface, out entry) ||

            //
            // Meg benne lehet generikus formaban.
            //

            iface.IsGenericType() && TryGet(iface.GetGenericTypeDefinition(), out entry);

        public virtual AbstractServiceEntry GetClosest(Type iface) => TryGetClosest(iface, out var result)
            ? result
            : throw new ServiceNotFoundException(iface);

        /// <summary>
        /// Gets the entry associated with the given interface.
        /// </summary>
        /// <param name="iface">The "id" of the entry. Must be an interface <see cref="Type"/>.</param>
        /// <returns>The stored <see cref="AbstractServiceEntry"/> instance.</returns>
        /// <remarks>This method supports entry specialization which means after registering a generic entry you can query its (unregistered) closed pair by passing the closed interface <see cref="Type"/> to this function.</remarks>
        public virtual AbstractServiceEntry Query(Type iface)
        {
            AbstractServiceEntry entry = GetClosest(iface);

            //
            // 1. eset: Azt az entitast adjuk vissza amit kerestunk.
            //

            if (entry.Interface == iface) return entry;

            //
            // 2. eset: Egy generikus bejegyzes lezart parjat kerdezzuk le
            //

            if (entry.IsGeneric())
            {
                //
                // 2a eset: A bejegyzesnek van beallitott gyar fv-e (pl Factory<TCica>() hivas) akkor nincs dolgunk.
                //

                if (entry.Factory != null) return entry;

                //
                // 2b eset: Konkretizalni kell a bejegyzest. Megjegyzendo h mentjuk is az uj bejegyzest igy a legkozelebb
                //          mar csak az elso esetig fog futni a Query().
                //

                //
                // Ha nem mi vagyunk a tulajdonosok akkor lekerdezzuk a tulajdonostol es masoljuk sajat magunkhoz
                // (a tulajdonos nyilvan rogzitani fogja az uj bejegyzest magahoz is).
                //

                if (entry.Owner != this) return entry
                    .Owner
                    .Query(iface)
                    .CopyTo(this);

                //
                // Ha mi vagyunk a tulajdonosok akkor nekunk kell lezarni a bejegyzest.
                //

                Add(entry = entry.Specialize(iface.GetGenericArguments()));
                return entry;
            }

            Debug.Fail("Failed to query an existing entry");
            return null;
        }
        #endregion

        #region ICollection
        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public virtual IEnumerator<AbstractServiceEntry> GetEnumerator() => FEntries.Values.GetEnumerator();

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Adds an entry to the collection.
        /// </summary>
        /// <param name="item">The <see cref="AbstractServiceEntry"/> instance to be added.</param>
        /// <remarks>A <see cref="ServiceAlreadyRegisteredException"/> is thrown if an item with the same interface already exists in the collection.</remarks>
        public virtual void Add(AbstractServiceEntry item)
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
        public virtual void CopyTo(AbstractServiceEntry[] array, int arrayIndex)
        {
            int i = 0; 
            foreach (AbstractServiceEntry entry in this)
            {
                array.SetValue(entry, i++ + arrayIndex);
            }
        }

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        public virtual bool Contains(AbstractServiceEntry item) =>
            //
            // Itt keruljuk a ContainsValue() hivast mert az Equals() by desgin tulajdonsag osszehasnlitassal mukodik
            //

            ContainsByRef(item);

        /// <summary>
        /// See <see cref="ICollection{T}"/>
        /// </summary>
        /// <remarks>Removing an item will NOT dipose it.</remarks>
        public virtual bool Remove(AbstractServiceEntry item) =>
            ContainsByRef(item) &&

            //
            // A ContainsByRef() miatt "item" sose null ezert nem kell h a resharper
            // baszogasson miatta.
            //

            // ReSharper disable once PossibleNullReferenceException
            FEntries.Remove(item.Interface);

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