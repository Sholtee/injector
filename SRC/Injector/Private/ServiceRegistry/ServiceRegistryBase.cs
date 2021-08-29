/********************************************************************************
* ServiceRegistryBase.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal abstract class ServiceRegistryBase : Composite<IServiceRegistry>, IServiceRegistry
    {
        protected static Resolver GenericEntryResolverFactory(int index, AbstractServiceEntry originalEntry) =>
            (self, iface, name) => self.ResolveGenericEntry(index, iface, originalEntry);

        protected static Resolver RegularEntryResolverFactory(int index, AbstractServiceEntry originalEntry) =>
            (self, iface, name) => self.ResolveRegularEntry(index, originalEntry);

        protected static ResolverBuilder GetResolverBuilder(IEnumerable<AbstractServiceEntry> entries) =>
            //
            // Kis elemszamnal -ha nem kell specializalni- a ResolverBuilder.ChainedDelegates joval gyorsabb (lasd teljesitmeny tesztek)
            //

            entries.Count() <= 50 && !entries.Any(entry => entry.Interface.IsGenericTypeDefinition)
                ? ResolverBuilder.ChainedDelegates
                : ResolverBuilder.CompiledExpression;

        protected static T[] CreateArray<T>(Func<T> factory, int count)
        {
            T[] result = new T[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = factory();
            }

            return result;
        }

        protected abstract ICollection<AbstractServiceEntry> UsedEntries { get; }

        protected abstract Resolver BuiltResolver { get; }

        protected abstract AbstractServiceEntry ResolveRegularEntry(int index, AbstractServiceEntry originalEntry);

        protected abstract AbstractServiceEntry ResolveGenericEntry(int index, Type specializedInterface, AbstractServiceEntry originalEntry);

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

        public new ServiceRegistryBase? Parent => (ServiceRegistryBase?) base.Parent;

        public AbstractServiceEntry? GetEntry(Type iface, string? name) => BuiltResolver.Invoke(this, Ensure.Parameter.IsNotNull(iface, nameof(iface)), name);

        public IReadOnlyList<AbstractServiceEntry> RegisteredEntries { get; }
    }
}
