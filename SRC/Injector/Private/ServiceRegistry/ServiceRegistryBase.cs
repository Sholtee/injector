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

    internal abstract class ServiceRegistryBase : Composite<IServiceRegistry>, IServiceRegistry
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Resolver GenericEntryResolverFactory(int index, AbstractServiceEntry originalEntry) =>
            (self, iface, name) => self.ResolveGenericEntry(index, iface, originalEntry);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static Resolver RegularEntryResolverFactory(int index, AbstractServiceEntry originalEntry) =>
            (self, iface, name) => self.ResolveRegularEntry(index, originalEntry);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ResolverBuilder GetResolverBuilder(IEnumerable<AbstractServiceEntry> entries) =>
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

        protected ServiceRegistryBase(IEnumerable<AbstractServiceEntry> entries, int maxChildCount) : base(maxChildCount: maxChildCount)
        {
            Ensure.Parameter.IsNotNull(entries, nameof(entries));

            RegisteredEntries = entries.ToArray();
        }

        protected ServiceRegistryBase(ServiceRegistryBase parent) : base(parent, parent.MaxChildCount) => RegisteredEntries = parent.RegisteredEntries;

        IServiceRegistry? IServiceRegistry.Parent => (IServiceRegistry?) Parent;

        public AbstractServiceEntry? GetEntry(Type iface, string? name) => BuiltResolver.Invoke(this, Ensure.Parameter.IsNotNull(iface, nameof(iface)), name);

        public IReadOnlyList<AbstractServiceEntry> RegisteredEntries { get; }

        public abstract AbstractServiceEntry ResolveRegularEntry(int index, AbstractServiceEntry originalEntry);

        public abstract AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry);

        public abstract ICollection<AbstractServiceEntry> UsedEntries { get; }

        public abstract Resolver BuiltResolver { get; }

        public abstract int RegularEntryCount { get; }

        public abstract int GenericEntryCount { get; }
    }
}
