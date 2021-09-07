/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
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

        private readonly Dictionary<Type, AbstractServiceEntry>[] FSpecializedEntries;

        private sealed class RegistryCollection : LinkedList<IServiceRegistry>, ICollection<IServiceRegistry>
        {
            void ICollection<IServiceRegistry>.Add(IServiceRegistry item)
            {
                LinkedListNode<IServiceRegistry> node = AddLast(item);

                item.OnDispose += (_, _) => Remove(node);
            }
        }
        #endregion

        #region Protected
        protected override ICollection<AbstractServiceEntry> UsedEntries { get; } = new List<AbstractServiceEntry>();

        protected override ICollection<IServiceRegistry> DerivedRegistries { get; } = new RegistryCollection();

        protected override void Dispose(bool disposeManaged)
        {
            if (DerivedRegistries.Count > 0)
            {
                //
                // Elemet felsorolas kozben nem tudunk eltavolitani a listabol ezert masolatot keszitunk eloszor
                // (ami bar lassit a torteneten de nem szalbiztos registry-nek elvileg nem is lesz leszarmazottja).
                //

                IServiceRegistry[] registries = new IServiceRegistry[DerivedRegistries.Count];
                DerivedRegistries.CopyTo(registries, 0);

                for (int i = 0; i < registries.Length; i++)
                {
                    registries[i].Dispose();
                }
            }

            base.Dispose(disposeManaged);
        }

        protected async override ValueTask AsyncDispose()
        {
            if (DerivedRegistries.Count > 0)
            {
                IServiceRegistry[] registries = new IServiceRegistry[DerivedRegistries.Count];
                DerivedRegistries.CopyTo(registries, 0);

                for (int i = 0; i < registries.Length; i++)
                {
                    await registries[i].DisposeAsync();
                }
            }

            await base.AsyncDispose();
        }
        #endregion

        #region Public
        public ServiceRegistry(ISet<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder = null, CancellationToken cancellation = default) : base(entries)
        {
            resolverBuilder ??= GetDefaultResolverBuilder(entries);

            BuiltResolver = resolverBuilder.Build(RegisteredEntries, RegularEntryResolverFactory, GenericEntryResolverFactory, out int reCount, out int geCount, cancellation);

            FRegularEntries = new AbstractServiceEntry?[reCount];
            FSpecializedEntries = CreateArray(() => new Dictionary<Type, AbstractServiceEntry>(), geCount);
        }

        public ServiceRegistry(ServiceRegistryBase parent) : base(Ensure.Parameter.IsNotNull(parent, nameof(parent)))
        {
            BuiltResolver = parent.BuiltResolver;

            FRegularEntries = new AbstractServiceEntry?[parent.RegularEntryCount];
            FSpecializedEntries = CreateArray(() => new Dictionary<Type, AbstractServiceEntry>(), parent.GenericEntryCount);
        }

        public override AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry)
        {
            if (specializedInterface.IsGenericTypeDefinition)
                throw new InvalidOperationException(); // TODO

            Dictionary<Type, AbstractServiceEntry> specializedEntries = FSpecializedEntries[index];

            if (!specializedEntries.TryGetValue(specializedInterface, out AbstractServiceEntry specializedEntry))
            {
                ISupportsSpecialization supportsSpecialization = (ISupportsSpecialization) originalEntry;

                specializedEntry = supportsSpecialization.Specialize(this, specializedInterface.GenericTypeArguments);

                UsedEntries.Add(specializedEntry);
                specializedEntries.Add(specializedInterface, specializedEntry);
            }

            return specializedEntry;
        }

        public override AbstractServiceEntry ResolveRegularEntry(int index, AbstractServiceEntry originalEntry)
        {
            ref AbstractServiceEntry? value = ref FRegularEntries[index];

            if (value is null)
            {    
                value = originalEntry.CopyTo(this);
                UsedEntries.Add(value);
            }

            return value;
        }

        public override Resolver BuiltResolver { get; }

        public override int RegularEntryCount => FRegularEntries.Length;

        public override int GenericEntryCount => FSpecializedEntries.Length;
        #endregion
    }
}
