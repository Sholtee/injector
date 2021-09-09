/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using System.Threading.Tasks;

    internal class ServiceRegistry : ServiceRegistryBase
    {
        #region Private
        private readonly AbstractServiceEntry?[] FRegularEntries;

        //
        // Ez NE egyetlen szotar legyen mert ott a neveket is szamon kene tartsuk amivel viszont
        // jelentosen lassulna a bejegyzes lekerdezes.
        //

        private readonly HybridDictionary?[] FSpecializedEntries;

        private readonly List<AbstractServiceEntry> FUsedEntries = new();

        private readonly RegistryCollection FDerivedRegistries = new();

        private sealed class RegistryCollection : LinkedList<IServiceRegistry>, IReadOnlyCollection<IServiceRegistry>
        {
            public void Add(IServiceRegistry item)
            {
                LinkedListNode<IServiceRegistry> node = AddFirst(item);

                item.OnDispose += (_, _) =>
                {
                    if (node.List is not null)
                        Remove(node);
                };
            }
        }
        #endregion

        #region Protected
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                for (LinkedListNode<IServiceRegistry> registry; (registry = FDerivedRegistries.First) is not null;)
                {
                    registry.Value.Dispose();
                }

                Debug.Assert(FDerivedRegistries.Count == 0, "DerivedRegistries is not empty");

                for (int i = 0; i < FUsedEntries.Count; i++)
                {
                    FUsedEntries[i].Dispose();
                }
            }

            base.Dispose(disposeManaged);
        }

        protected async override ValueTask AsyncDispose()
        {
            for (LinkedListNode<IServiceRegistry> registry; (registry = FDerivedRegistries.First) is not null;)
            {
                await registry.Value.DisposeAsync();
            }

            Debug.Assert(FDerivedRegistries.Count == 0, "DerivedRegistries is not empty");

            for (int i = 0; i < FUsedEntries.Count; i++)
            {
                await FUsedEntries[i].DisposeAsync();
            }

            await base.AsyncDispose();
        }

        protected override void AddChild(IServiceRegistry registry) => FDerivedRegistries.Add(registry);
        #endregion

        #region Public
        public ServiceRegistry(ISet<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder = null, CancellationToken cancellation = default) : base(entries)
        {
            resolverBuilder ??= GetDefaultResolverBuilder(entries);

            BuiltResolver = resolverBuilder.Build(RegisteredEntries, RegularEntryResolverFactory, GenericEntryResolverFactory, out int reCount, out int geCount, cancellation);

            FRegularEntries = new AbstractServiceEntry?[reCount];
            FSpecializedEntries = new HybridDictionary?[geCount];
        }

        public ServiceRegistry(ServiceRegistryBase parent) : base(Ensure.Parameter.IsNotNull(parent, nameof(parent)))
        {
            BuiltResolver = parent.BuiltResolver;

            FRegularEntries = new AbstractServiceEntry?[parent.RegularEntryCount];
            FSpecializedEntries = new HybridDictionary?[parent.GenericEntryCount];
        }

        public override AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry)
        {
            Debug.Assert(specializedInterface.IsConstructedGenericType, $"{nameof(specializedInterface)} must be a closed generic type");

            ref HybridDictionary? specializedEntries = ref FSpecializedEntries[index];
            if (specializedEntries is null)
                specializedEntries = new HybridDictionary();

            AbstractServiceEntry? specializedEntry = (AbstractServiceEntry?) specializedEntries[specializedInterface];

            if (specializedEntry is null)
            {
                ISupportsSpecialization supportsSpecialization = (ISupportsSpecialization) originalEntry;

                specializedEntry = supportsSpecialization.Specialize(this, specializedInterface.GenericTypeArguments);
                specializedEntries[specializedInterface] = specializedEntry;

                FUsedEntries.Add(specializedEntry);
            }

            return specializedEntry;
        }

        public override AbstractServiceEntry ResolveRegularEntry(int index, AbstractServiceEntry originalEntry)
        {
            ref AbstractServiceEntry? value = ref FRegularEntries[index];

            if (value is null)
            {    
                value = originalEntry.CopyTo(this);
                FUsedEntries.Add(value);
            }

            return value;
        }

        public override Resolver BuiltResolver { get; }

        public override IReadOnlyCollection<AbstractServiceEntry> UsedEntries => FUsedEntries;

        public override IReadOnlyCollection<IServiceRegistry> DerivedRegistries => FDerivedRegistries;

        public override int RegularEntryCount => FRegularEntries.Length;

        public override int GenericEntryCount => FSpecializedEntries.Length;
        #endregion
    }
}
