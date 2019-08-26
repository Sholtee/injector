/********************************************************************************
* ConcurrentServiceCollection.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    internal class ConcurrentServiceCollection: ServiceCollection
    {
        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

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
                // Ha vmiert kezzel akarnank felvinni mar regisztralt elemet akkor itt mivel az ost hivjuk
                // az egyszeruen ujra fogja dobni a ServiceAlreadyRegisteredException-t szoval jol kezeli
                // ezt az esetet is.
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
                // ToArray() kell h masolatot adjunk vissza
                //

                return ((IEnumerable<ServiceEntry>) this.ToArray()).GetEnumerator();
            }
        }

        public override void CopyTo(ServiceEntry[] array, int arrayIndex)
        {
            using (FLock.AcquireReaderLock())
            {
                base.CopyTo(array, arrayIndex);
            }
        }

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
