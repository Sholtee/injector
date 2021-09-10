/********************************************************************************
* ServiceRegistryBase.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        protected static Resolver GenericEntryResolverFactory(int index, AbstractServiceEntry originalEntry) =>
            (self, iface, name) => self.ResolveGenericEntry(index, iface, originalEntry);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Resolver RegularEntryResolverFactory(int index, AbstractServiceEntry originalEntry) =>
            (self, iface, name) => self.ResolveRegularEntry(index, originalEntry);

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

        [SuppressMessage("Usage", "CA2214:Do not call overridable methods in constructors")]
        protected ServiceRegistryBase(ServiceRegistryBase parent, bool register) : base()
        {
            Ensure.Parameter.IsNotNull(parent, nameof(parent));

            RegisteredEntries = parent.RegisteredEntries;
            Parent = parent;

            if (register)
                parent.AddChild(this);
        }

        protected abstract void AddChild(IServiceRegistry registry);

        protected virtual IReadOnlyCollection<AbstractServiceEntry> BuiltInServices { get; } = Array.Empty<AbstractServiceEntry>();

        public IServiceRegistry? Parent { get; }

        public AbstractServiceEntry? GetEntry(Type iface, string? name) => BuiltResolver.Invoke(this, Ensure.Parameter.IsNotNull(iface, nameof(iface)), name);

        public IReadOnlyCollection<AbstractServiceEntry> RegisteredEntries { get; }

        public abstract IReadOnlyCollection<IServiceRegistry> DerivedRegistries { get; }

        public abstract IReadOnlyCollection<AbstractServiceEntry> UsedEntries { get; }

        public abstract AbstractServiceEntry ResolveRegularEntry(int index, AbstractServiceEntry originalEntry);

        public abstract AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry);

        public abstract Resolver BuiltResolver { get; }

        public abstract int RegularEntryCount { get; }

        public abstract int GenericEntryCount { get; }
    }
}
