/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    /// <summary>
    /// Implements the <see cref="IServiceRegistry"/> interface.
    /// </summary>
    public class ServiceRegistry : Composite<IServiceRegistry>, IServiceRegistry
    {
        #region Private
        private readonly Resolver FResolver;

        private readonly EntryHolder[] FRegularEntries;

        private readonly ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>[] FSpecializedEntries;

        private readonly ConcurrentCollection<AbstractServiceEntry> FUsedEntries = new(); // Add() lehet hivva parhuzamosan

        private static T[] CreateArray<T>(Func<T> factory, int count)
        {
            T[] result = new T[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = factory();
            }

            return result;
        }

        private sealed class EntryHolder
        {
            public AbstractServiceEntry? Value;
        }
        #endregion

        #region Protected
        /// <summary>
        /// Returns a <see cref="Resolver"/> that is responsible for resolving regular enries.
        /// </summary>
        protected virtual Resolver RegularEntryResolverFactory(int index, AbstractServiceEntry entry) => (self, iface, name) =>
        {
            EntryHolder holder = self.FRegularEntries[index];

            if (holder.Value is null)
            {
                lock (holder) // entry csak egyszer legyen masolva
                {
                    if (holder.Value is null)
                    {
                        holder.Value = entry.Copy();
                        self.FUsedEntries.Add(holder.Value);
                    }
                }
            }

            return holder.Value;
        };

        /// <summary>
        /// Returns a <see cref="Resolver"/> that is responsible for resolving generic enries.
        /// </summary>
        protected virtual Resolver GenericEntryResolverFactory(int index, AbstractServiceEntry entry)
        {
            if (entry is not ISupportsSpecialization supportsSpecialization)
                throw new InvalidOperationException(); // TODO

            return (self, iface, name) =>
            {
                ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>> specializedEntries = self.FSpecializedEntries[index];

                //
                // Lazy azert kell mert ha ugyanarra a kulcsra parhuzamosan kerul meghivasra a GetOrAdd() akkor a factory
                // tobbszor is meg lehet hivva (lasd MSDN).
                //

                return specializedEntries
                    .GetOrAdd(iface, _ => new Lazy<AbstractServiceEntry>(Specialize, LazyThreadSafetyMode.ExecutionAndPublication))
                    .Value;

                AbstractServiceEntry Specialize()
                {
                    AbstractServiceEntry specializedEntry = supportsSpecialization.Specialize(iface.GenericTypeArguments);

                    self.FUsedEntries.Add(specializedEntry);
                    return specializedEntry;
                }
            };
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                foreach (AbstractServiceEntry usedEntry in FUsedEntries)
                {
                    usedEntry.Dispose();
                }
            }
            base.Dispose(disposeManaged);
        }

        /// <inheritdoc/>
        protected async override ValueTask AsyncDispose()
        {
            await Task.WhenAll
            (
                FUsedEntries.Select(usedEntry => usedEntry.DisposeAsync().AsTask())
            );

            await base.AsyncDispose();
        }
        #endregion

        /// <summary>
        /// Creates a new <see cref="ServiceRegistry"/> instance.
        /// </summary>
        public ServiceRegistry(IEnumerable<AbstractServiceEntry> entries, ResolverBuilder resolverBuilder, int maxChildCount = int.MaxValue) : base(maxChildCount: maxChildCount)
        {
            Ensure.Parameter.IsNotNull(entries, nameof(entries));
            Ensure.Parameter.IsNotNull(resolverBuilder, nameof(resolverBuilder));

            RegisteredEntries = entries.ToArray(); // elsonek legyen inicialva [BuildResolver() hasznalja]

            FResolver = resolverBuilder.Build(RegisteredEntries, RegularEntryResolverFactory, GenericEntryResolverFactory, out int reCount, out int geCount);
            FRegularEntries = CreateArray(() => new EntryHolder(), reCount);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), geCount);
        }

        /// <summary>
        /// Creates a new <see cref="ServiceRegistry"/> instance.
        /// </summary>
        public ServiceRegistry(IEnumerable<AbstractServiceEntry> entries, int maxChildCount = int.MaxValue) : this
        (
            entries,

            //
            // Kis elemszamnal -ha nem kell specializalni- a ResolverBuilder.ChainedDelegates joval gyorsabb (lasd teljesitmeny tesztek)
            //

            entries.Count() <= 50 && !entries.Any(entry => entry.Interface.IsGenericTypeDefinition)
                ? ResolverBuilder.ChainedDelegates
                : ResolverBuilder.CompiledExpression,
            
            maxChildCount
        ) { }

            /// <summary>
        /// Creates a new <see cref="ServiceRegistry"/> instance.
        /// </summary>
        public ServiceRegistry(ServiceRegistry parent): base(Ensure.Parameter.IsNotNull(parent, nameof(parent)), parent.MaxChildCount)
        {
            RegisteredEntries = parent.RegisteredEntries;

            FResolver = parent.FResolver;
            FRegularEntries = CreateArray(() => new EntryHolder(), parent.FRegularEntries.Length);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), parent.FSpecializedEntries.Length);
        }

        /// <summary>
        /// The parent of this entry.
        /// </summary>
        public new ServiceRegistry? Parent => (ServiceRegistry?) base.Parent;

        /// <inheritdoc/>
        public AbstractServiceEntry? GetEntry(Type iface, string? name) => FResolver.Invoke(this, Ensure.Parameter.IsNotNull(iface, nameof(iface)), name);

        /// <inheritdoc/>
        public IReadOnlyList<AbstractServiceEntry> RegisteredEntries { get; }
    }
}
