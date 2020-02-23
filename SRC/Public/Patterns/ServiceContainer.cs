/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if NETSTANDARD1_6
using System.Reflection;
#endif
using System.Threading;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI
{
    using Properties;
    using Internals;

    /// <summary>
    /// Implements the <see cref="IServiceContainer"/> interface.
    /// </summary>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "The name provides meaningful information about the implementation")]
    public class ServiceContainer : Composite<IServiceContainer>, IServiceContainer
    {
        private readonly Dictionary<IServiceId, AbstractServiceEntry> FEntries;

        //
        // Singleton elettartamnal parhuzamosan is modositasra kerulhet a  bejegyzes lista 
        // (generikus bejegyzes lezarasakor) ezert szalbiztosnak kell h legyen.
        //

        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim();

        private bool ShouldDispose(AbstractServiceEntry entry) => entry.Owner == this;

        #region IServiceContainer
        /// <summary>
        /// See <see cref="IServiceContainer.Add"/>.
        /// </summary>
        public IServiceContainer Add(AbstractServiceEntry entry)
        {
            CheckDisposed();

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            using (FLock.AcquireWriterLock())
            {
                //
                // Abstract bejegyzest felul lehet irni (de csak azt).
                //

                if (FEntries.TryGetValue(entry, out AbstractServiceEntry entryToRemove) && entryToRemove.GetType() == typeof(AbstractServiceEntry))
                {
                    bool removed = FEntries.Remove(entryToRemove);
                    Assert(removed, "Can't remove entry");

                    if (ShouldDispose(entryToRemove))
                        entryToRemove.Dispose();
                }

                //
                // Uj elem felvetele.
                //

                try
                {
                    FEntries.Add(entry, entry);
                }
                catch (ArgumentException e)
                {
                    throw new ServiceAlreadyRegisteredException(entry, e);
                }
            }

            return this;
        }

        /// <summary>
        /// See <see cref="IServiceContainer.Get"/>.
        /// </summary>
        public AbstractServiceEntry Get(Type serviceInterface, string name, QueryModes mode)
        {
            CheckDisposed();

            if (serviceInterface == null)
                throw new ArgumentNullException(nameof(serviceInterface));

            if (!serviceInterface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(serviceInterface));

            IServiceId key = MakeId(serviceInterface);

            AbstractServiceEntry result;

            using (FLock.AcquireReaderLock())
            {
                //
                // 1. eset: Vissza tudjuk adni amit kerestunk.
                //

                if (FEntries.TryGetValue(key, out result))
                    return result;

                //
                // 2. eset: A bejegyzes generikus parjat kell majd feldolgozni.
                //

                bool hasGenericEntry = mode.HasFlag(QueryModes.AllowSpecialization) &&
                                       serviceInterface.IsGenericType() &&
                                       FEntries.TryGetValue
                                       (
                                           MakeId(serviceInterface.GetGenericTypeDefinition()), 
                                           out result
                                       );

                //
                // 3. eset: Egyik se jott be, vagy kivetelt v NULL-t adunk vissza.
                //

                if (!hasGenericEntry)
                    return !mode.HasFlag(QueryModes.ThrowOnError) 
                        ? (AbstractServiceEntry) null 
                        : throw new ServiceNotFoundException(key);
            }

            Assert(result.IsGeneric());

            try
            {
                //
                // Ha nem mi vagyunk a tulajdonosok akkor ertesitjuk a tulajdonost h tipizalja o a bejegyzest
                // majd masoljuk az uj elemet sajat magunkhoz (epp ugy mint ha "orokoltuk" vna).
                //
                // Igy lehetove tesszuk h pl singleton elettartamnal a tipizalt peldany elettartamarol is a
                // deklaralo kollekcio gondoskodjon.
                //

                if (result.Owner != this)
                {
                    Assert(result.Owner != null, $"Entry without owner for {serviceInterface}");

                    return result
                        .Owner
                        .Get(serviceInterface, name, QueryModes.AllowSpecialization)

                        //
                        // A CopyTo() belsoleg a this.Add()-et fogja hivni, reszleteket lasd a kivetel kezeloben.
                        //

                        .CopyTo(this);
                }

                //
                // Ha mi vagyunk a tulajdonosok akkor nekunk kell tipizalni majd felvenni a bejegyzest.
                //

                Add(result = result.Specialize(serviceInterface.GetGenericArguments()));
                return result;
            }
            catch (ServiceAlreadyRegisteredException)
            {
                //
                // Ne a QueryModes.ThrowOnError-t hasznaljuk mert a bejegyzesnek itt mar leteznie KELL.
                //

                AbstractServiceEntry registered = Get(serviceInterface, name, QueryModes.Default);

                Assert(registered != null);

                //
                // - Normal mukodes mellett parhuzamos regisztracio csak nehany esetben elkepzelheto (pl ha Singleton 
                //   generikus lezart parjat igenyeljuk parhuzamosan, leszarmazott kontenerekbol).
                // - Kulomben az Add() "kivulrol" lett hivva parhuzamosan azonos Interface-Nev parossal.
                // - NE az AbstractServiceEntry.Equals() overload-jat hivjuk mert az figyelembe veszi a legyartott
                //   peldanyt is, ami parhuzamos esetben lehet kulombozo (NULL "result"-nal mig !NULL "registered"-nel).
                //

                Assert(ServiceDefinitionComparer.Instance.Equals(result, registered), "Unexpected concurrency");

                //
                // A feleslegesen legyartott peldany nem kell
                //

                Assert(ShouldDispose(result));

                result.Dispose();

                return registered;
            }

            IServiceId MakeId(Type iface) => new ServiceId
            {
                Interface = iface,
                Name = name
            };
        }

        /// <summary>
        /// The number of the elements in this <see cref="IServiceContainer"/>.
        /// </summary>
        public int Count
        {
            get
            {
                CheckDisposed();

                using (FLock.AcquireReaderLock())
                {
                    return FEntries.Count;
                }
            }
        }
        #endregion

        #region IEnumerable
        /// <summary>
        /// Returns a new <see cref="IEnumerator{AbstractServiceEntry}"/> instance that enumerates on the shallow copy of this instance.
        /// </summary>
        /// <returns>The newly crated <see cref="IEnumerator{AbstractServiceEntry}"/> instance.</returns>
        public IEnumerator<AbstractServiceEntry> GetEnumerator()
        {
            CheckDisposed();

            using (FLock.AcquireReaderLock())
            {
                //
                // Masolatot adjunk vissza.
                //

                return ((IEnumerable<AbstractServiceEntry>) FEntries.Values.ToArray()).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Protected
        /// <summary>
        /// Creates a new <see cref="ServiceContainer"/> instance copying the entries from the <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">The parent <see cref="IServiceContainer"/>.</param>
        protected ServiceContainer(IServiceContainer parent) : base(parent)
        {
            FEntries = new Dictionary<IServiceId, AbstractServiceEntry>(parent?.Count ?? 0, ServiceIdComparer.Instance);
            if (parent == null) return;

            foreach (AbstractServiceEntry entry in parent)
            {
                entry.CopyTo(this);
            }
        }

        /// <summary>
        /// Disposes this instance freeing all the owned entries.
        /// </summary>
        /// <param name="disposeManaged">See <see cref="Disposable.Dispose(bool)"/>.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Itt ne "this.Where()"-t hivjunk mert az felesleges hivna egy ToArray()-t is
                // (lasd GetEnumerator() implementacio).
                //

                foreach (IDisposable disposable in FEntries.Values.Where(ShouldDispose))
                {
                    disposable.Dispose();
                }

                FEntries.Clear();

                FLock.Dispose();
            }

            base.Dispose(disposeManaged);
        }   
        #endregion

        #region Public
        /// <summary>
        /// Creates a new <see cref="ServiceContainer"/> instance.
        /// </summary>
        public ServiceContainer() : this(null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="IServiceContainer"/> child.
        /// </summary>
        /// <returns>The newly created child.</returns>
        public override IServiceContainer CreateChild()
        {
            CheckDisposed();

            return new ServiceContainer(this);
        }
        #endregion
    }
}