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

        public ConcurrentServiceCollection(IReadOnlyCollection<AbstractServiceEntry> inheritedEntries): base(inheritedEntries)
        {           
        }

        public override void Add(AbstractServiceEntry item)
        {
            using (FLock.AcquireWriterLock())
            {
                base.Add(item);
            }           
        }

        //
        // Get-et nem kell felulirni mert az is a TryGet()-et hivja.
        //

        public override bool TryGet(Type iface, out AbstractServiceEntry entry)
        {
            using (FLock.AcquireReaderLock())
            {
                return base.TryGet(iface, out entry);
            }            
        }

        public override bool TryGetClosest(Type iface, out AbstractServiceEntry entry)
        {
            using (FLock.AcquireReaderLock())
            {
                return base.TryGetClosest(iface, out entry);
            }
        }

        public override bool Remove(AbstractServiceEntry item)
        {
            using (FLock.AcquireWriterLock())
            {
                return base.Remove(item);
            }
        }

        public override AbstractServiceEntry Query(Type iface)
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

        public override bool Contains(AbstractServiceEntry item)
        {
            using (FLock.AcquireReaderLock())
            {
                return base.Contains(item);
            }
        }

        public override IEnumerator<AbstractServiceEntry> GetEnumerator()
        {
            using (FLock.AcquireReaderLock())
            {
                //
                // Masolatot adjunk vissza. Megjegyzes: NE this.ToArray()-t hasznaljunk hogy
                // elkeruljuk a SOException-t
                //

                var ar = new AbstractServiceEntry[Count];

                using (IEnumerator<AbstractServiceEntry> enumerator = base.GetEnumerator())
                {
                    for (int i = 0; enumerator.MoveNext(); i++)
                    {
                        ar[i] = enumerator.Current;
                    }
                }

                return ((IEnumerable<AbstractServiceEntry>) ar).GetEnumerator();
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
