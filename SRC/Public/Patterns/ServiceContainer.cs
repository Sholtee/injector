/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
    using Properties;
    using Internals;

    /// <summary>
    /// Implements the <see cref="IServiceContainer"/> interface.
    /// </summary>
    public class ServiceContainer : Composite<IServiceContainer>, IServiceContainer
    {
        private readonly Dictionary<(Type Interface, string Name), AbstractServiceEntry> FEntries;

        //
        // Singleton elettartamnal parhuzamosan is modositasra kerulhet a  bejegyzes lista 
        // (generikus bejegyzes lezarasakor) ezert szalbiztosnak kell h legyen.
        //

        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim();

        #region IServiceContainer
        /// <summary>
        /// See <see cref="IServiceContainer.Add"/>.
        /// </summary>
        public IServiceContainer Add(AbstractServiceEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            using (FLock.AcquireWriterLock())
            {
                var key = (entry.Interface, entry.Name);

                //
                // Abstract bejegyzest felul lehet irni (de csak azt).
                //

                if (FEntries.TryGetValue(key, out var entryToRemove) && entryToRemove.GetType() == typeof(AbstractServiceEntry))
                {
                    bool removed = FEntries.Remove(key);
                    Debug.Assert(removed, "Can't remove entry");
                }

                //
                // Uj elem felvetele.
                //

                try
                {
                    FEntries.Add(key, entry);
                }
                catch (ArgumentException e)
                {
                    throw new ServiceAlreadyRegisteredException(entry.Interface, e);
                }
            }

            return this;
        }

        /// <summary>
        /// See <see cref="IServiceContainer.Get"/>.
        /// </summary>
        public AbstractServiceEntry Get(Type serviceInterface, string name, QueryMode mode)
        {
            if (serviceInterface == null)
                throw new ArgumentNullException(nameof(serviceInterface));

            if (!serviceInterface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(serviceInterface));

            AbstractServiceEntry result;

            using (FLock.AcquireReaderLock())
            {
                //
                // 1. eset: Vissza tudjuk adni amit kerestunk.
                //

                if (FEntries.TryGetValue((serviceInterface, name), out result))
                    return result;

                //
                // 2. eset: A bejegyzes generikus parjat kell majd feldolgozni.
                //

                bool hasGenericEntry = mode.HasFlag(QueryMode.AllowSpecialization) &&
                                       serviceInterface.IsGenericType() &&
                                       FEntries.TryGetValue
                                       (
                                           (serviceInterface.GetGenericTypeDefinition(), name), 
                                           out result
                                       );

                //
                // 3. eset: Egyik se jott be, vagy kivetelt v NULL-t adunk vissza.
                //

                if (!hasGenericEntry)
                    return !mode.HasFlag(QueryMode.ThrowOnError) ? (AbstractServiceEntry) null : throw new ServiceNotFoundException(serviceInterface);
            }

            Debug.Assert(result.IsGeneric());

            //
            // Ha van factory fv akkor nincs dolgunk [pl Factory(typeof(IGeneric<>), ...) hivas utan lehet ilyen bejegyzes]
            //
            // TODO: TBD: Ennek kulon flag?
            //

            if (result.Factory != null)
                return result;

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
                    Debug.Assert(result.Owner != null, $"Entry without owner for {serviceInterface}");

                    return result
                        .Owner
                        .Get(serviceInterface, name, QueryMode.AllowSpecialization)

                        //
                        // A CopyTo() belsoleg a this.Add()-et fogja hivni, reszleteket lasd a kivetel kezeloben.
                        //

                        .CopyTo(this);
                }

                //
                // Ha mi vagyunk a tulajdonosok akkor nekunk kell tipizalni majd felvenni a bejegyzest.
                //

                Debug.Assert(result.Implementation != null, $"Naked generic entry for {serviceInterface}");

                Add(result = result.Specialize(serviceInterface.GetGenericArguments()));
                return result;
            }
            catch (ServiceAlreadyRegisteredException)
            {
                //
                // Parhuzamos esetben az Add() dobhat ServiceAlreadyRegisteredException-t ekkor 
                // egyszeruen visszaadjuk a masik szal altal regisztralt peldanyt.
                //

                return Get(serviceInterface, name, QueryMode.ThrowOnError);
            }
        }

        /// <summary>
        /// The number of the elements in this <see cref="IServiceContainer"/>.
        /// </summary>
        public int Count
        {
            get
            {
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
            FEntries = new Dictionary<(Type Interface, string Name), AbstractServiceEntry>(parent?.Count ?? 0);
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

                foreach (IDisposable disposable in FEntries.Values.Where(entry => entry.Owner == this))
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
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
        public override IServiceContainer CreateChild() => new ServiceContainer(this);
        #endregion
    }
}