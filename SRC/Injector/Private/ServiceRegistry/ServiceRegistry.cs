/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ServiceRegistry : ServiceRegistryBase
    {
        private readonly AbstractServiceEntry?[] FRegularEntries;

        private readonly Dictionary<Type, AbstractServiceEntry>[] FSpecializedEntries;

        protected override ICollection<AbstractServiceEntry> UsedEntries { get; } = new List<AbstractServiceEntry>();

        protected override Resolver BuiltResolver { get; }

        protected override AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry)
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

        protected override AbstractServiceEntry ResolveRegularEntry(int index, AbstractServiceEntry originalEntry)
        {
            ref AbstractServiceEntry? value = ref FRegularEntries[index];

            if (value is null)
            {    
                value = originalEntry.CopyTo(this);
                UsedEntries.Add(value);
            }

            return value;
        }

        public ServiceRegistry(IEnumerable<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder = null, int maxChildCount = int.MaxValue) : base(entries, maxChildCount)
        {
            resolverBuilder ??= GetResolverBuilder(entries);

            BuiltResolver = resolverBuilder.Build(RegisteredEntries, RegularEntryResolverFactory, GenericEntryResolverFactory, out int reCount, out int geCount);

            FRegularEntries = new AbstractServiceEntry?[reCount];
            FSpecializedEntries = CreateArray(() => new Dictionary<Type, AbstractServiceEntry>(), geCount);
        }

        public ServiceRegistry(ServiceRegistry parent) : base(Ensure.Parameter.IsNotNull(parent, nameof(parent)))
        {
            BuiltResolver = parent.BuiltResolver;

            FRegularEntries = new AbstractServiceEntry?[parent.FRegularEntries.Length];
            FSpecializedEntries = CreateArray(() => new Dictionary<Type, AbstractServiceEntry>(), parent.FSpecializedEntries.Length);
        }

        public override string ToString() => nameof(ServiceRegistry);
    }
}
