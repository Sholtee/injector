/********************************************************************************
* ServiceResolverLookup_BuiltBTree.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed partial class ServiceResolverLookup_BuiltBTree : ServiceResolverLookup_BTree
    {
        private readonly Func<long, string?, AbstractServiceEntry?> FGetGenericEntry;

        private volatile Func<long, string?, ServiceResolver?> FGetResolver;

        protected override void AddResolver(ServiceResolver resolver)
        {
            base.AddResolver(resolver);

            //
            // During initialization phase "FGetResolver" & "FGetGenericEntry" is intentionally null
            // to avoid unnecessary BuildSwitch() calls.
            //

            if (FInitialized)
                FGetResolver = BuildSwitch(FGetResolverSwitch);
        }

        protected override bool TryGetResolver(Type iface, string? name, out ServiceResolver resolver)
        {
            if (!FInitialized)
                return base.TryGetResolver(iface, name, out resolver);

            resolver = FGetResolver((long) iface.TypeHandle.Value, name)!;
            return resolver is not null;
        }

        protected override bool TryGetGenericEntry(Type iface, string? name, out AbstractServiceEntry genericEntry)
        {
            if (!FInitialized)
                return base.TryGetGenericEntry(iface, name, out genericEntry);

            genericEntry = FGetGenericEntry((long) iface.TypeHandle.Value, name)!;
            return genericEntry is not null;
        }

        public new const string Id = "builtbtree";

        public ServiceResolverLookup_BuiltBTree(IEnumerable<AbstractServiceEntry> entries, ScopeOptions scopeOptions) : base(entries, scopeOptions)
        {
            FGetGenericEntry = BuildSwitch(FGetGenericEntrySwitch);
            FGetResolver = BuildSwitch(FGetResolverSwitch);
        }
    }
}
