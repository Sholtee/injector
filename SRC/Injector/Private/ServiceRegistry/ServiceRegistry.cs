﻿/********************************************************************************
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

    internal class ServiceRegistry : ServiceRegistryBase
    {
        private readonly AbstractServiceEntry?[] FRegularEntries;

        private readonly Dictionary<Type, AbstractServiceEntry>[] FSpecializedEntries;

        public ServiceRegistry(IEnumerable<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder = null, int maxChildCount = int.MaxValue, CancellationToken cancellation = default) : base(entries, maxChildCount)
        {
            resolverBuilder ??= GetResolverBuilder(entries);

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

        public override ICollection<AbstractServiceEntry> UsedEntries { get; } = new List<AbstractServiceEntry>();

        public override Resolver BuiltResolver { get; }

        public override int RegularEntryCount => FRegularEntries.Length;

        public override int GenericEntryCount => FSpecializedEntries.Length;
    }
}
