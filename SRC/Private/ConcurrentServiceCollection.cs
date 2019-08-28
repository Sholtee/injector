/********************************************************************************
* ConcurrentServiceCollection.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    internal class ConcurrentServiceCollection: ServiceCollection
    {
        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public ConcurrentServiceCollection(IReadOnlyCollection<ServiceEntry> inheritedEntries): base(inheritedEntries)
        {           
        }

        protected override ServiceEntry QueryInternal(Type iface)
        {
            using (FLock.AcquireReaderLock())
            {
                return base.QueryInternal(iface);
            }
        }

        public override void Add(ServiceEntry item)
        {
            using (FLock.AcquireWriterLock())
            {
                base.Add(item);
            }           
        }

        public override ServiceEntry Query(Type iface)
        {
            try
            {
                return base.Query(iface);
            }
            catch (ServiceAlreadyRegisteredException)
            {
                //
                // Ez itt viccesen nez ki viszont a motorhazteto alatt a Query() rogzithet is uj elemet
                // (generikus bejegyzes lekerdezesekor) ami parhuzamos esetben dobhat kivetelt. Ilyenkor
                // visszaadjuk a masik szal altal regisztralt bejegyzest.
                //

                return base.Query(iface);
            }           
        }

        public override void Clear()
        {
            using (FLock.AcquireWriterLock())
            {
                base.Clear();
            }        
        }

        public override bool Contains(ServiceEntry item)
        {
            using (FLock.AcquireReaderLock())
            {
                return base.Contains(item);
            }
        }

        public override IEnumerator<ServiceEntry> GetEnumerator()
        {
            using (FLock.AcquireReaderLock())
            {
                //
                // Masolatot adjunk vissza. Megjegyzes: NE this.ToArray()-t hasznaljunk hogy
                // elkeruljuk a SOException-t
                //

                var ar = new ServiceEntry[Count];

                using (IEnumerator<ServiceEntry> enumerator = base.GetEnumerator())
                {
                    for (int i = 0; enumerator.MoveNext(); i++)
                    {
                        ar[i] = enumerator.Current;
                    }
                }

                return ((IEnumerable<ServiceEntry>) ar).GetEnumerator();
            }
        }

        //
        // Nem kell override-olni mert az os ugy is a felulirt GetEnumerator()-t fogja hasznalni.
        //
/*
        public override void CopyTo(ServiceEntry[] array, int arrayIndex)
        {
            using (FLock.AcquireReaderLock())
            {
                base.CopyTo(array, arrayIndex);
            }
        }
*/
        public override int Count
        {
            get
            {
                using (FLock.AcquireReaderLock())
                {
                    return base.Count;
                }
            }
        }
    }
}
