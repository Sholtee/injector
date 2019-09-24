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

        public override AbstractServiceEntry Get(Type iface, QueryMode mode = QueryMode.Default)
        {
            using (FLock.AcquireReaderLock())
            {
                return base.Get(iface, mode);
            }            
        }

        public override bool Remove(AbstractServiceEntry item)
        {
            using (FLock.AcquireWriterLock())
            {
                return base.Remove(item);
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
