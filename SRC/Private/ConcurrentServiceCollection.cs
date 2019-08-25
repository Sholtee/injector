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

        public override ServiceEntry QueryEntry(Type iface)
        {
            ServiceEntry entry, genericEntry;

            using (FLock.AcquireReaderLock())
            {
                if (QueryEntry(iface, out entry)) return entry;

                if (!iface.IsGenericType() || !QueryEntry(iface.GetGenericTypeDefinition(), out genericEntry))
                    throw new ServiceNotFoundException(iface);

                if (genericEntry.Factory != null) return genericEntry;
            }

            try
            {
                Add(entry = genericEntry.Specialize(iface.GetGenericArguments()));
                return entry;
            }
            catch (ServiceAlreadyRegisteredException)
            {
                return this.QueryEntry(iface);
            }

            bool QueryEntry(Type key, out ServiceEntry val) => FEntries.TryGetValue(key, out val);
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
