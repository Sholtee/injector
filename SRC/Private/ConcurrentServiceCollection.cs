/********************************************************************************
* ConcurrentServiceCollection.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    internal class ConcurrentServiceCollection: ServiceCollection
    {
        private readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public override void Add(ServiceEntry item)
        {
            using (FLock.AcquireWriterLock())
            {
                base.Add(item);
            }           
        }

        public override ServiceEntry Query(Type iface)
        {
            ServiceEntry genericEntry;

            using (FLock.AcquireReaderLock())
            {
                if (Query(iface, out var entry)) return entry;

                if (!iface.IsGenericType() || !Query(iface.GetGenericTypeDefinition(), out genericEntry))
                    throw new ServiceNotFoundException(iface);

                if (genericEntry.Factory != null) return genericEntry;
            }

            try
            {
                return genericEntry.Specialize(iface.GetGenericArguments());
            }
            catch (ServiceAlreadyRegisteredException)
            {
                return this.Query(iface);
            }

            bool Query(Type key, out ServiceEntry val) => FEntries.TryGetValue(key, out val);
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
