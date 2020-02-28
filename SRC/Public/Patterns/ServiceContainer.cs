/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if NETSTANDARD1_6
using System.Reflection;
#endif
using System.Threading;

namespace Solti.Utils.DI
{
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

        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private bool ShouldDispose(AbstractServiceEntry entry) => entry.Owner == this;

        #region IServiceContainer
        /// <summary>
        /// See <see cref="IServiceContainer.Add"/>.
        /// </summary>
        public IServiceContainer Add(AbstractServiceEntry entry)
        {
            Ensure.Parameter.IsNotNull(entry, nameof(entry));
            Ensure.NotDisposed(this);

            using (FLock.AcquireWriterLock())
            {
                if (FEntries.TryGetValue(entry, out AbstractServiceEntry existing))
                {
                    //
                    // Abstract bejegyzest felul lehet irni (de csak azt).
                    //

                    if (existing.GetType() != typeof(AbstractServiceEntry)) 
                        throw new ServiceAlreadyRegisteredException(existing);

                    if (ShouldDispose(existing))
                        existing.Dispose();
                }

                //
                // Ha volt mar bejegyzes adott kulccsal akkor felulijra kulomben hozzaadja.
                //

                FEntries[entry] = entry;
            }

            return this;
        }

        /// <summary>
        /// See <see cref="IServiceContainer.Get"/>.
        /// </summary>
        public AbstractServiceEntry Get(Type serviceInterface, string name, QueryModes mode)
        {
            Ensure.Parameter.IsNotNull(serviceInterface, nameof(serviceInterface));
            Ensure.Parameter.IsInterface(serviceInterface, nameof(serviceInterface));
            Ensure.NotDisposed(this);

            IServiceId key = MakeId(serviceInterface);

            AbstractServiceEntry existing;

            using (FLock.AcquireReaderLock())
            {
                //
                // 1. eset: Vissza tudjuk adni amit kerestunk.
                //

                if (FEntries.TryGetValue(key, out existing))
                    return existing;

                //
                // 2. eset: A bejegyzes generikus parjat kell majd feldolgozni.
                //

                bool hasGenericEntry = mode.HasFlag(QueryModes.AllowSpecialization) &&
                                       serviceInterface.IsGenericType() &&
                                       FEntries.TryGetValue
                                       (
                                           MakeId(serviceInterface.GetGenericTypeDefinition()), 
                                           out existing
                                       );

                //
                // 3. eset: Egyik se jott be, vagy kivetelt v NULL-t adunk vissza.
                //

                if (!hasGenericEntry)
                    return !mode.HasFlag(QueryModes.ThrowOnError) 
                        ? (AbstractServiceEntry) null 
                        : throw new ServiceNotFoundException(key);
            }

            Debug.Assert(existing.IsGeneric());

            using (FLock.AcquireWriterLock())
            {
                //
                // Kozben vki berakta mar?
                //

                if (FEntries.TryGetValue(key, out AbstractServiceEntry specialized))
                    return specialized;

                //
                // Ha nem mi vagyunk a tulajdonosok akkor ertesitjuk a tulajdonost h tipizalja o a bejegyzest
                // majd masoljuk az uj elemet sajat magunkhoz (epp ugy mint ha "orokoltuk" vna).
                //
                // Igy lehetove tesszuk h pl singleton elettartamnal a tipizalt peldany elettartamarol is a
                // deklaralo kollekcio gondoskodjon.
                //

                if (existing.Owner != this)
                {
                    //
                    // Bejegyzesek "kivulrol" jonnek -> ne Assert() hivas legyen.
                    //

                    Ensure.IsNotNull(existing.Owner, $"{nameof(existing)}.{nameof(existing.Owner)}");

                    return existing
                        .Owner
                        .Get(serviceInterface, name, QueryModes.AllowSpecialization)

                        //
                        // A CopyTo() is a this.Add()-et fogja belsoleg hivni.
                        //

                        .CopyTo(this);
                }

                //
                // Ha mi vagyunk a tulajdonosok akkor nekunk kell tipizalni majd felvenni a bejegyzest.
                //

                Add(specialized = existing.Specialize(serviceInterface.GetGenericArguments()));
                return specialized;
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
                Ensure.NotDisposed(this);

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
            Ensure.NotDisposed(this);

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

        /// <summary>
        /// Creates a new <see cref="ServiceContainer"/> instance copying the entries from the <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">The parent <see cref="IServiceContainer"/>.</param>
        internal ServiceContainer(IServiceContainer parent) : base(parent)
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

        /// <summary>
        /// Creates a new <see cref="ServiceContainer"/> instance.
        /// </summary>
        public ServiceContainer() : this(null)
        {
        }
    }
}