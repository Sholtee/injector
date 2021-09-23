/********************************************************************************
* ServiceRegistryBase.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Properties;

    //                                        !!!FIGYELEM!!!
    //
    // Ez az osztaly kozponti komponens, ezert minden modositast korultekintoen, a teljesitmenyt szem elott tartva
    // kell elvegezni:
    // - nincs Sysmte.Linq
    // - nincs System.Reflection
    // - mindig futtassuk a teljesitmeny teszteket (is) hogy a hatekonysag nem romlott e
    //

    internal abstract class ServiceRegistryBase : DisposableSupportsNotifyOnDispose, IServiceRegistry
    {
        private static Resolver GenericEntryResolverFactory(int slot, AbstractServiceEntry originalEntry) =>
            (self, iface, name) => self.ResolveGenericEntry(slot, iface, originalEntry);

        private static Resolver RegularEntryResolverFactory(int slot, AbstractServiceEntry originalEntry) =>
            (self, iface, name) => self.ResolveRegularEntry(slot, originalEntry);

        protected ServiceRegistryBase(ISet<AbstractServiceEntry> entries, ResolverBuilder? resolverBuilder, CancellationToken cancellation) : base()
        {
            Ensure.Parameter.IsNotNull(entries, nameof(entries));

            int oldLength = entries.Count;

            entries.UnionWith(BuiltInServices);

            if (entries.Count != oldLength + BuiltInServices.Count)
                throw new ArgumentException(Resources.BUILT_IN_SERVICE_OVERRIDE, nameof(entries));

            RegisteredEntries = entries as IReadOnlyCollection<AbstractServiceEntry> ?? entries.ToArray(); // HashSet megvalositja az IReadOnlyCollection-t

            resolverBuilder ??= RegisteredEntries.Count <= 100
                //
                // Teljesitmenytesztek alapjan...
                //

                ? ResolverBuilder.CompiledExpression
                : ResolverBuilder.CompiledCode;

            BuiltResolver = resolverBuilder.Build(RegisteredEntries, RegularEntryResolverFactory, GenericEntryResolverFactory, out int reCount, out int geCount, cancellation);
            RegularEntryCount = reCount;
            GenericEntryCount = geCount;
        }

        protected ServiceRegistryBase(ServiceRegistryBase parent) : base()
        {
            Ensure.Parameter.IsNotNull(parent, nameof(parent));

            Parent = parent;
            RegisteredEntries = parent.RegisteredEntries;
            BuiltResolver     = parent.BuiltResolver;
            RegularEntryCount = parent.RegularEntryCount;
            GenericEntryCount = parent.GenericEntryCount;
        }

        protected virtual IReadOnlyCollection<AbstractServiceEntry> BuiltInServices { get; } = Array.Empty<AbstractServiceEntry>();

        public IServiceRegistry? Parent { get; }

        public AbstractServiceEntry? GetEntry(Type iface, string? name) => BuiltResolver.Invoke(this, Ensure.Parameter.IsNotNull(iface, nameof(iface)), name);

        public IReadOnlyCollection<AbstractServiceEntry> RegisteredEntries { get; }

        public abstract AbstractServiceEntry ResolveRegularEntry(int slot, AbstractServiceEntry originalEntry);

        public abstract AbstractServiceEntry ResolveGenericEntry(int slot, Type specializedInterface, AbstractServiceEntry originalEntry);

        public Resolver BuiltResolver { get; }

        public int RegularEntryCount { get; }

        public int GenericEntryCount { get; }
    }
}
