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
        private readonly Dictionary<Type, AbstractServiceEntry> FEntries;

        //
        // Singleton elettartamnal parhuzamosan is modositasra kerulhet a  bejegyzes lista 
        // (generikus bejegyzes lezarasakor) ezert szalbiztosnak kell h legyen.
        //

        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim();

        #region IServiceContainer
        public IServiceContainer Add(AbstractServiceEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            using (FLock.AcquireWriterLock())
            {
                //
                // Abstract bejegyzest felul lehet irni (de csak azt).
                //

                if (FEntries.TryGetValue(entry.Interface, out var entryToRemove) && entryToRemove.GetType() == typeof(AbstractServiceEntry))
                {
                    bool removed = FEntries.Remove(entry.Interface);
                    Debug.Assert(removed, "Can't remove entry");
                }

                //
                // Uj elem felvetele.
                //

                try
                {
                    FEntries.Add(entry.Interface, entry);
                }
                catch (ArgumentException e)
                {
                    throw new ServiceAlreadyRegisteredException(entry.Interface, e);
                }
            }

            return this;
        }

        public AbstractServiceEntry Get(Type serviceInterface, QueryMode mode = QueryMode.Default)
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

                if (FEntries.TryGetValue(serviceInterface, out result))
                    return result;

                //
                // 2. eset: A bejegyzes generikus parjat kell majd feldolgozni.
                //

                bool hasGenericEntry = mode.HasFlag(QueryMode.AllowSpecialization) &&
                                       serviceInterface.IsGenericType() &&
                                       FEntries.TryGetValue(serviceInterface.GetGenericTypeDefinition(), out result);

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
                        .Get(serviceInterface, QueryMode.AllowSpecialization)

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

                return Get(serviceInterface, QueryMode.ThrowOnError);
            }
        }

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
        protected ServiceContainer(IServiceContainer parent) : base(parent)
        {
            FEntries = new Dictionary<Type, AbstractServiceEntry>(parent?.Count ?? 0);
            if (parent == null) return;

            foreach (AbstractServiceEntry entry in parent)
            {
                entry.CopyTo(this);
            }
        }

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

        public override IServiceContainer CreateChild() => new ServiceContainer(this);
        #endregion
    }
}