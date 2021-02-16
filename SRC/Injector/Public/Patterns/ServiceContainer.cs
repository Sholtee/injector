/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;
    using Primitives.Threading;
    using Properties;

    /// <summary>
    /// Implements the <see cref="IServiceContainer"/> interface.
    /// </summary>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "The name provides meaningful information about the implementation")]
    public class ServiceContainer : Composite<IServiceContainer>, IServiceContainer
    {
        /// <summary>
        /// Indicates that the entry exists for internal use.
        /// </summary>
        public const string INTERNAL_SERVICE_NAME_PREFIX = "$";

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
        public virtual void Add(AbstractServiceEntry entry)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(entry, nameof(entry));

            using (FLock.AcquireWriterLock())
            {
                if (FEntries.TryGetValue(entry, out AbstractServiceEntry existing))
                {
                    //
                    // Abstract bejegyzest felul lehet irni (de csak azt).
                    //

                    if (existing.GetType() != typeof(AbstractServiceEntry)) 
                        throw new ServiceAlreadyRegisteredException(string.Format(Resources.Culture, Resources.ALREADY_REGISTERED, existing.FriendlyName()));

                    if (ShouldDispose(existing))
                        //
                        // "AbstractServiceEntry"-nel nincs tenyleges Dispose() logika csak azert van hivva 
                        // h ne legyen figyelmeztetes a debug kimeneten -> DisposeAsync() hivasnak se lenne 
                        // ertelme.
                        //

                        existing.Dispose();
                }

                //
                // Ha volt mar bejegyzes adott kulccsal akkor felulijra kulomben hozzaadja.
                //

                FEntries[entry] = entry;
            }
        }

        /// <summary>
        /// See <see cref="IServiceContainer.Get"/>.
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "The identifier won't confuse the users of the API.")]
        public virtual AbstractServiceEntry? Get(Type serviceInterface, string? name, QueryModes mode)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(serviceInterface, nameof(serviceInterface));
            Ensure.Parameter.IsInterface(serviceInterface, nameof(serviceInterface));

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
                                       serviceInterface.IsGenericType &&
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
                        ? (AbstractServiceEntry?) null 
                        : throw new ServiceNotFoundException(string.Format(Resources.Culture, Resources.SERVICE_NOT_FOUND, key.FriendlyName()));
            }

            Assert(existing.IsGeneric());
            Assert(mode.HasFlag(QueryModes.AllowSpecialization));

            using (FLock.AcquireWriterLock())
            {
                //
                // Kozben vki berakta mar?
                //

                if (FEntries.TryGetValue(key, out AbstractServiceEntry? specialized))
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
                    specialized = existing
                        .Owner
                        .Get(serviceInterface, name, mode);

                    //
                    // "specialized" lehet NULL ha a "QueryModes.ThrowOnError" nem volt beallitva es "existing"
                    // nem valositja meg a "ISupportsSpecialization" interface-t.
                    //

                    return specialized?.CopyTo(this);
                }

                //
                // Ha mi vagyunk a tulajdonosok akkor nekunk kell tipizalni majd felvenni a bejegyzest.
                //

                if (existing is ISupportsSpecialization generic) 
                {
                    //
                    // Ne az "FEntries.Add(specialized, specialized)"-ot hivjuk mert a "this.Add()" virtualis.
                    //

                    Add(specialized = generic.Specialize(serviceInterface.GetGenericArguments()));
                    return specialized;
                }

                return !mode.HasFlag(QueryModes.ThrowOnError)
                    ? (AbstractServiceEntry?) null
                    : throw new NotSupportedException(Resources.ENTRY_CANNOT_BE_SPECIALIZED);
            }

            IServiceId MakeId(Type iface) => new ServiceId(iface, name);
        }

        /// <summary>
        /// The number of the elements in this <see cref="IServiceContainer"/>.
        /// </summary>
        public int Count
        {
            get
            {
                CheckNotDisposed();

                using (FLock.AcquireReaderLock())
                {
                    return FEntries.Count;
                }
            }
        }
        #endregion

        #region IEnumerable
        /// <summary>
        /// Returns a new <see cref="IEnumerator{AbstractServiceEntry}"/> instance that enumerates on this instance.
        /// </summary>
        /// <returns>The newly crated <see cref="IEnumerator{AbstractServiceEntry}"/> instance.</returns>
        /// <remarks>You must dispose the returned enumerator (which is automatically done in foreach loops).</remarks>
        public IEnumerator<AbstractServiceEntry> GetEnumerator()
        {
            CheckNotDisposed();

            //
            // A "FEntries.Values" nem masolat
            //

            return new SafeEnumerator<AbstractServiceEntry>(FEntries.Values, FLock);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Protected
        /// <summary>
        /// Creates a new <see cref="ServiceContainer"/> instance copying the entries from the <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">The parent <see cref="IServiceContainer"/>.</param>
        [SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors", Justification = "This is intended to make inheritance logic extensible.")]
        protected internal ServiceContainer(IServiceContainer? parent) : base(parent, Config.Value.ServiceContainer.MaxChildCount)
        {
            FEntries = new Dictionary<IServiceId, AbstractServiceEntry>(parent?.Count ?? 0, ServiceIdComparer.Instance);
            if (parent == null) return;

            try
            {
                foreach (AbstractServiceEntry entry in parent)
                {
                    entry.CopyTo(this);
                }
            }
            catch 
            {
                //
                // "base()" hivas miatt mar muszaly dispose-olni.
                //

                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Disposes this instance by freeing all the owned entries.
        /// </summary>
        /// <param name="disposeManaged">See <see cref="Disposable.Dispose(bool)"/>.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Itt ne "this.Where()"-t hivjunk mert az feleslegesen lock-olna.
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
        /// Asynchronously disposes this instance by freeing all the owned entries.
        /// </summary>
        protected async override ValueTask AsyncDispose()
        {
            await Task.WhenAll
            (
                FEntries
                    .Values
                    .Where(ShouldDispose)
                    .Select(disposable => disposable.DisposeAsync().AsTask())
            );

            FEntries.Clear();

            FLock.Dispose();

            await base.AsyncDispose();
        }
        #endregion

        /// <summary>
        /// Creates a new <see cref="ServiceContainer"/> instance.
        /// </summary>
        public ServiceContainer() : this(null)
        {
        }
    }
}