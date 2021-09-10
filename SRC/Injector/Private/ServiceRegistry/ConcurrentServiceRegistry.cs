/********************************************************************************
* ConcurrentServiceRegistry.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;

    internal class ConcurrentServiceRegistry : ServiceRegistryBase
    {
        #region Private
        private sealed class EntryHolder
        {
            public AbstractServiceEntry? Value;
        }

        private readonly EntryHolder[] FRegularEntries;

        //
        // Ez NE egyetlen szotar legyen mert ott a neveket is szamon kene tartsuk amivel viszont
        // jelentosen lassulna a bejegyzes lekerdezes.
        //

        private readonly ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>[] FSpecializedEntries;

        private readonly ConcurrentBag<AbstractServiceEntry> FUsedEntries = new();

        private readonly ConcurrentRegistryCollection FDerivedRegistries = new();

        private sealed class ConcurrentRegistryCollection : ConcurrentLinkedList<IServiceRegistry>, IReadOnlyCollection<IServiceRegistry>
        {
            public void Add(IServiceRegistry item)
            {
                LinkedListNode<IServiceRegistry> node = AddFirst(item);
                item.OnDispose += (_, _) =>
                {
                    //
                    // A Dispose() karakterisztikajabol adodoan ez a metodus biztosan csak egyszer lesz meghivva
                    //

                    if (node.Owner is not null) // Takefirst() mar kivehette a listabol
                        Remove(node);
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static T[] CreateArray<T>(Func<T> factory, int count)
        {
            T[] result = new T[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = factory();
            }

            return result;
        }
        #endregion

        #region Protected
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                while (FDerivedRegistries.TakeFirst(out IServiceRegistry registry))
                {
                    registry.Dispose();
                }

                while (FUsedEntries.TryTake(out AbstractServiceEntry entry))
                {
                    entry.Dispose();
                }
            }

            base.Dispose(disposeManaged);
        }

        protected async override ValueTask AsyncDispose()
        {
            while (FDerivedRegistries.TakeFirst(out IServiceRegistry registry))
            {
                await registry.DisposeAsync();
            }

            while (FUsedEntries.TryTake(out AbstractServiceEntry entry))
            {
                await entry.DisposeAsync();
            }

            await base.AsyncDispose();
        }

        protected override void AddChild(IServiceRegistry registry) => FDerivedRegistries.Add(registry);
        #endregion

        #region Public
        public ConcurrentServiceRegistry(ISet<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder = null, CancellationToken cancellation = default) : base(entries)
        {
            resolverBuilder ??= GetDefaultResolverBuilder(entries);

            BuiltResolver = resolverBuilder.Build(RegisteredEntries, RegularEntryResolverFactory, GenericEntryResolverFactory, out int reCount, out int geCount, cancellation);

            FRegularEntries = CreateArray(() => new EntryHolder(), reCount);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), geCount);
        }

        public ConcurrentServiceRegistry(ConcurrentServiceRegistry parent, bool register) : base(Ensure.Parameter.IsNotNull(parent, nameof(parent)), register)
        {
            BuiltResolver = parent.BuiltResolver;

            FRegularEntries = CreateArray(() => new EntryHolder(), parent.RegularEntryCount);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), parent.GenericEntryCount);
        }

        public override AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry)
        {
            Debug.Assert(specializedInterface.IsConstructedGenericType, $"{nameof(specializedInterface)} must be a closed generic type");

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

                FUsedEntries.Add(specializedEntry);
                return specializedEntry;
            }
        }

        public override AbstractServiceEntry ResolveRegularEntry(int index, AbstractServiceEntry originalEntry)
        {
            EntryHolder holder = FRegularEntries[index];

            if (holder.Value is null)
            {
                lock (holder) // entry csak egyszer legyen masolva
                {
                    if (holder.Value is null)
                    {
                        holder.Value = originalEntry.CopyTo(this);
                        FUsedEntries.Add(holder.Value);
                    }
                }
            }

            return holder.Value;
        }

        public override Resolver BuiltResolver { get; }

        public override IReadOnlyCollection<AbstractServiceEntry> UsedEntries => FUsedEntries;

        public override IReadOnlyCollection<IServiceRegistry> DerivedRegistries => FDerivedRegistries;

        public override int RegularEntryCount => FRegularEntries.Length;

        public override int GenericEntryCount => FSpecializedEntries.Length;
        #endregion
    }
}
