/********************************************************************************
* ConcurrentServiceRegistry.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private sealed class ConcurrentRegistryCollection : ConcurrentCollection<IServiceRegistry>
        {
            public override void Add(IServiceRegistry item)
            {
                LinkedListNode<IServiceRegistry> node = new() 
                { 
                    Value = item 
                };

                item.OnDispose += (_, _) => FUnderlyingList.Remove(node);

                FUnderlyingList.Add(node);
            }
        }
        #endregion

        #region Protected
        protected override ICollection<AbstractServiceEntry> UsedEntries { get; } = new ConcurrentCollection<AbstractServiceEntry>();

        protected override ICollection<IServiceRegistry> DerivedRegistries { get; } = new ConcurrentRegistryCollection();

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Felsorolas kozben nem lehet listabol eltavolitani az aktualis elemet utana viszont mar siman.
                // Ha kozben magan a leszarmazotton is hivva volt a Dispose() akkor sincs gond, ez esetben az
                // ujabb Dispose() hivas mar nem csinal semmit.
                //

                if (DerivedRegistries.Count > 0)
                {
                    IServiceRegistry? previous = null;

                    foreach (IServiceRegistry current in DerivedRegistries)
                    {
                        previous?.Dispose();
                        previous = current;
                    }

                    previous?.Dispose();
                }
            }

            base.Dispose(disposeManaged);
        }

        protected async override ValueTask AsyncDispose()
        {
            if (DerivedRegistries.Count > 0)
            {
                IServiceRegistry? previous = null;

                foreach (IServiceRegistry current in DerivedRegistries)
                {
                    if (previous is not null)
                        await previous.DisposeAsync();

                    previous = current;
                }

                if (previous is not null)
                    await previous.DisposeAsync();
            }

            await base.AsyncDispose();
        }
        #endregion

        #region Public
        public ConcurrentServiceRegistry(ISet<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder = null, CancellationToken cancellation = default) : base(entries)
        {
            resolverBuilder ??= GetDefaultResolverBuilder(entries);

            BuiltResolver = resolverBuilder.Build(RegisteredEntries, RegularEntryResolverFactory, GenericEntryResolverFactory, out int reCount, out int geCount, cancellation);

            FRegularEntries = CreateArray(() => new EntryHolder(), reCount);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), geCount);
        }

        public ConcurrentServiceRegistry(ConcurrentServiceRegistry parent) : base(Ensure.Parameter.IsNotNull(parent, nameof(parent)))
        {
            BuiltResolver = parent.BuiltResolver;

            FRegularEntries = CreateArray(() => new EntryHolder(), parent.RegularEntryCount);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), parent.GenericEntryCount);
        }

        public override AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry)
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
                        UsedEntries.Add(holder.Value);
                    }
                }
            }

            return holder.Value;
        }

        public override Resolver BuiltResolver { get; }

        public override int RegularEntryCount => FRegularEntries.Length;

        public override int GenericEntryCount => FSpecializedEntries.Length;
        #endregion
    }
}
