/********************************************************************************
* ExperimentalServiceRegistry.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed class ExperimentalServiceRegistry: Disposable, IServiceRegistry
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct ServiceId
        {
            [FieldOffset(0)]
            public readonly long Value;

            [FieldOffset(0)]
            public readonly int IfaceHash;

            //
            // Nem masoljuk a teljes nevet mert lassu allandoan masolatot kesziteni belole
            //

            [FieldOffset(4)]
            public readonly int NameHash;

            public ServiceId(Type iface, in string? name)
            {
                Value = 0;
                IfaceHash = iface.GetHashCode();
                NameHash = name?.GetHashCode
                (
                    #if NETSTANDARD2_1_OR_GREATER
                    StringComparison.Ordinal
                    #endif
                ) ?? 0;
            }

            public ServiceId(Type iface, in long value)
            {
                NameHash = 0;
                Value = value;
                IfaceHash = iface.GetHashCode();
            }
        }

        private readonly ConcurrentDictionary<long, AbstractServiceEntry?> FEntries;

        private readonly ExperimentalServiceRegistry? FParent;

        private readonly int FInitialCount;

        public ExperimentalServiceRegistry(ICollection<AbstractServiceEntry> entries)
        {
            FEntries = new ConcurrentDictionary<long, AbstractServiceEntry?>
            (
                concurrencyLevel: Environment.ProcessorCount,
                capacity: FInitialCount = entries.Count
            );

            foreach (AbstractServiceEntry entry in entries)
            {
                FEntries.TryAdd
                (
                    new ServiceId(entry.Interface, entry.Name).Value,
                    entry
                );
            }
        }

        public ExperimentalServiceRegistry(ExperimentalServiceRegistry parent, int concurrencyLevel = 1) // leszarmazottat elvileg nem baszogat tobb szal
        {
            FEntries = new ConcurrentDictionary<long, AbstractServiceEntry?>
            (
                concurrencyLevel,

                //
                // Elemszam lekerdezes lassu lehet:
                // https://github.com/dotnet/runtime/blob/cd5beb14b0ce11ae1e4e3f1d30bffba7e37ce606/src/libraries/System.Collections.Concurrent/src/System/Collections/Concurrent/ConcurrentDictionary.cs#L1068
                //

                capacity: FInitialCount = parent.FInitialCount
            );
            FParent = parent;
        }

        private AbstractServiceEntry? ResolveCore(Type iface, in long serviceIdVal)
        {
            if (FParent is not null)
            {
                AbstractServiceEntry? existing = FParent.ResolveEntry(iface, serviceIdVal);
                return existing is null || existing.IsShared
                    ? existing
                    : existing.WithOwner(this);
            }        

            if (iface.IsGenericType)
            {
                ServiceId genericId = new(iface.GetGenericTypeDefinition(), serviceIdVal);

                if (FEntries.TryGetValue(genericId.Value, out AbstractServiceEntry? genericEntry) && genericEntry is ISupportsSpecialization supportsSpecialization)
                    return supportsSpecialization.Specialize
                    (
                        this,
                        iface.GenericTypeArguments
                    );
            }

            return null;
        }

        public IReadOnlyCollection<AbstractServiceEntry> RegisteredEntries => throw new NotImplementedException();

        public IServiceRegistry? Parent => FParent;

        private AbstractServiceEntry? ResolveEntry(Type iface, in long serviceId) => FEntries.GetOrAdd(serviceId, serviceId => ResolveCore(iface, serviceId));

        public AbstractServiceEntry ResolveEntry(Type iface, in string? name) => ResolveEntry(iface, new ServiceId(iface, name).Value) ?? new MissingServiceEntry(iface, name);

        public AbstractServiceEntry GetEntry(Type iface, string? name) // todo: "in string? name"
        {
            throw new NotImplementedException();
        }

        public AbstractServiceEntry ResolveRegularEntry(int slot, AbstractServiceEntry originalEntry)
        {
            throw new NotImplementedException();
        }

        public AbstractServiceEntry ResolveGenericEntry(int slot, Type specializedInterface, AbstractServiceEntry originalEntry)
        {
            throw new NotImplementedException();
        }
    }
}
