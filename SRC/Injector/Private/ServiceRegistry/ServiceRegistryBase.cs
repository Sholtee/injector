/********************************************************************************
* ServiceRegistryBase.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                foreach (AbstractServiceEntry usedEntry in UsedEntries)
                {
                    usedEntry.Dispose();
                }
            }
            base.Dispose(disposeManaged);
        }

        protected async override ValueTask AsyncDispose()
        {
            await Task.WhenAll
            (
                UsedEntries.Select(usedEntry => usedEntry.DisposeAsync().AsTask())
            );
            await base.AsyncDispose();
        }

        protected ServiceRegistryBase(ISet<AbstractServiceEntry> entries) : base()
        {
            Ensure.Parameter.IsNotNull(entries, nameof(entries));

            int oldLength = entries.Count;

            entries.UnionWith(ContextualServices);

            if (entries.Count != oldLength + ContextualServices.Count)
                throw new ArgumentException(Resources.BUILT_IN_SERVICE_OVERRIDE, nameof(entries));

            RegisteredEntries = entries as IReadOnlyCollection<AbstractServiceEntry> ?? entries.ToArray(); // HashSet megvalositja az IReadOnlyCollection-t
        }

        protected ServiceRegistryBase(ServiceRegistryBase parent) : base()
        {
            Ensure.Parameter.IsNotNull(parent, nameof(parent));

            RegisteredEntries = parent.RegisteredEntries;
            Parent = parent;

            parent.DerivedRegistries.Add(this);
        }

        protected virtual IReadOnlyCollection<AbstractServiceEntry> ContextualServices { get; } = Array.Empty<AbstractServiceEntry>();

        protected abstract ICollection<AbstractServiceEntry> UsedEntries { get; }

        protected abstract ICollection<IServiceRegistry> DerivedRegistries { get; }

        IReadOnlyCollection<IServiceRegistry> IServiceRegistry.DerivedRegistries => (IReadOnlyCollection<IServiceRegistry>) DerivedRegistries;

        public IServiceRegistry? Parent { get; }

        public AbstractServiceEntry? GetEntry(Type iface, string? name) => BuiltResolver.Invoke(this, Ensure.Parameter.IsNotNull(iface, nameof(iface)), name);

        public IReadOnlyCollection<AbstractServiceEntry> RegisteredEntries { get; }

        public abstract AbstractServiceEntry ResolveRegularEntry(int index, AbstractServiceEntry originalEntry);

        public abstract AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry);

        public abstract Resolver BuiltResolver { get; }

        public abstract int RegularEntryCount { get; }

        public abstract int GenericEntryCount { get; }
    }
}
