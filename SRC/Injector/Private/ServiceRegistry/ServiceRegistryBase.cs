/********************************************************************************
* ServiceRegistryBase.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Properties;

    internal abstract class ServiceRegistryBase : DisposableSupportsNotifyOnDispose, IServiceRegistry
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Resolver GenericEntryResolverFactory(int slot, AbstractServiceEntry originalEntry) =>
            (self, iface, name) => self.ResolveGenericEntry(slot, iface, originalEntry);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Resolver RegularEntryResolverFactory(int slot, AbstractServiceEntry originalEntry) =>
            (self, iface, name) => self.ResolveRegularEntry(slot, originalEntry);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ResolverBuilder GetDefaultResolverBuilder(IEnumerable<AbstractServiceEntry> entries) =>
            //
            // Teljesitmenytesztek alapjan...
            //

            entries.Count() <= 100
                ? ResolverBuilder.CompiledExpression
                : ResolverBuilder.CompiledCode;

        protected ServiceRegistryBase(ISet<AbstractServiceEntry> entries) : base()
        {
            Ensure.Parameter.IsNotNull(entries, nameof(entries));

            int oldLength = entries.Count;

            entries.UnionWith(BuiltInServices);

            if (entries.Count != oldLength + BuiltInServices.Count)
                throw new ArgumentException(Resources.BUILT_IN_SERVICE_OVERRIDE, nameof(entries));

            RegisteredEntries = entries as IReadOnlyCollection<AbstractServiceEntry> ?? entries.ToArray(); // HashSet megvalositja az IReadOnlyCollection-t
        }

        protected ServiceRegistryBase(ServiceRegistryBase parent) : base()
        {
            Ensure.Parameter.IsNotNull(parent, nameof(parent));

            RegisteredEntries = parent.RegisteredEntries;
            Parent = parent;
        }

        protected virtual IReadOnlyCollection<AbstractServiceEntry> BuiltInServices { get; } = Array.Empty<AbstractServiceEntry>();

        public IServiceRegistry? Parent { get; } // TODO: torolni / atmozgatni

        public AbstractServiceEntry? GetEntry(Type iface, string? name) => BuiltResolver.Invoke(this, Ensure.Parameter.IsNotNull(iface, nameof(iface)), name);

        public IReadOnlyCollection<AbstractServiceEntry> RegisteredEntries { get; }

        public abstract AbstractServiceEntry ResolveRegularEntry(int slot, AbstractServiceEntry originalEntry);

        public abstract AbstractServiceEntry ResolveGenericEntry(int slot, Type specializedInterface, AbstractServiceEntry originalEntry);

        public abstract Resolver BuiltResolver { get; }

        public abstract int RegularEntryCount { get; }

        public abstract int GenericEntryCount { get; }
    }
}
