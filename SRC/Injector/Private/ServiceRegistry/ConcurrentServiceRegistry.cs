/********************************************************************************
* ConcurrentServiceRegistry.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ConcurrentServiceRegistry : ServiceRegistryBase
    {
        private sealed class EntryHolder
        {
            public AbstractServiceEntry? Value;
        }

        private readonly EntryHolder[] FRegularEntries;

        private readonly ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>[] FSpecializedEntries;

        protected override ICollection<AbstractServiceEntry> UsedEntries { get; } = new ConcurrentCollection<AbstractServiceEntry>();

        protected override Resolver BuiltResolver { get; }

        protected override AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry)
        {
            if (specializedInterface.IsGenericTypeDefinition)
                throw new InvalidOperationException(); // TODO

            ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>> specializedEntries = FSpecializedEntries[index];

            //
            // Lazy azert kell mert ha ugyanarra a kulcsra parhuzamosan kerul meghivasra a GetOrAdd() akkor a factory
            // tobbszor is meg lehet hivva (lasd MSDN).
            //

            return specializedEntries
                .GetOrAdd(specializedInterface, _ => new Lazy<AbstractServiceEntry>(Specialize, LazyThreadSafetyMode.ExecutionAndPublication))
                .Value;

            AbstractServiceEntry Specialize()
            {
                ISupportsSpecialization supportsSpecialization = (ISupportsSpecialization) originalEntry;

                AbstractServiceEntry specializedEntry = supportsSpecialization.Specialize(this, specializedInterface.GenericTypeArguments);

                UsedEntries.Add(specializedEntry);
                return specializedEntry;
            }
        }

        protected override AbstractServiceEntry ResolveRegularEntry(int index, AbstractServiceEntry originalEntry)
        {
            EntryHolder holder = FRegularEntries[index];

            if (holder.Value is null)
            {
                lock (holder) // entry csak egyszer legyen masolva
                {
                    if (holder.Value is null)
                    {
                        holder.Value = originalEntry.CopyTo(this);
                        UsedEntries.Add(holder.Value);
                    }
                }
            }

            return holder.Value;
        }

        public ConcurrentServiceRegistry(IEnumerable<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder = null, int maxChildCount = int.MaxValue) : base(entries, maxChildCount)
        {
            resolverBuilder ??= GetResolverBuilder(entries);

            BuiltResolver = resolverBuilder.Build(RegisteredEntries, RegularEntryResolverFactory, GenericEntryResolverFactory, out int reCount, out int geCount);

            FRegularEntries = CreateArray(() => new EntryHolder(), reCount);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), geCount);
        }

        public ConcurrentServiceRegistry(ConcurrentServiceRegistry parent) : base(Ensure.Parameter.IsNotNull(parent, nameof(parent)))
        {
            BuiltResolver = parent.BuiltResolver;

            FRegularEntries = CreateArray(() => new EntryHolder(), parent.FRegularEntries.Length);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), parent.FSpecializedEntries.Length);
        }

        public override string ToString() => nameof(ConcurrentServiceRegistry);
    }
}
