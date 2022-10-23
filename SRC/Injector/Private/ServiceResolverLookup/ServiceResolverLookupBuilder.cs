/********************************************************************************
* ServiceResolverLookupBuilder.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class ServiceResolverLookupBuilder
    {
        //
        // According to performance tests, up to ~50 items built btree is faster than dictionary.
        // Assuming that there won't be more than 20 constructed generic service 30 seems a good
        // threshold.
        //

        public const int BTREE_ITEM_THRESHOLD = 30;

        public const string BTREE = nameof(BTREE);

        public const string DICT = nameof(DICT);

        public static IServiceResolverLookup Build(ICollection<AbstractServiceEntry> entries, ScopeOptions scopeOptions)
        {
            BatchedDelegateCompiler delegateCompiler = new();

            #pragma warning disable CA1304 // Specify CultureInfo
            IServiceResolverLookup lookup = (scopeOptions.Engine?.ToUpper() ?? (entries.Count <= BTREE_ITEM_THRESHOLD ? BTREE : DICT)) switch
            #pragma warning restore CA1304 // Specify CultureInfo
            {
                DICT => CreateInitialLookup<DictionaryLookup<ServiceResolver>, DictionaryLookup<AbstractServiceEntry>>().ChangeBackend
                (
                    static _ => _,
                    static _ => _,
                    CreateGraphBuilderFactory
                    (
                        new SimpleDelegateCompiler()
                    )
                ),
                BTREE => CreateInitialLookup<BTreeLookup<ServiceResolver>, BTreeLookup<AbstractServiceEntry>>().ChangeBackend
                (
                    btree => btree.Compile(delegateCompiler),
                    btree => btree.Compile(delegateCompiler),
                    CreateGraphBuilderFactory
                    (
                        new SimpleDelegateCompiler()
                    )
                ),
                _ => throw new NotSupportedException()
            };

            delegateCompiler.Compile();

            return lookup;

            ConcurrentServiceResolverLookup<TResolverLookup, TEntryLookup> CreateInitialLookup<TResolverLookup, TEntryLookup>()
                where TResolverLookup : class, ILookup<ServiceResolver, TResolverLookup>, new()
                where TEntryLookup : class, ILookup<AbstractServiceEntry, TEntryLookup>, new()
            =>
                new ConstructableConcurrentServiceResolverLookup<TResolverLookup, TEntryLookup>
                (
                    entries,
                    CreateGraphBuilderFactory(delegateCompiler)
                );

            Func<IServiceResolverLookup, IGraphBuilder> CreateGraphBuilderFactory(IDelegateCompiler compiler) =>
                lookup => scopeOptions.ServiceResolutionMode switch
                {
                    ServiceResolutionMode.JIT => new ShallowDependencyGraphBuilder(compiler),
                    ServiceResolutionMode.AOT => new RecursiveDependencyGraphBuilder(lookup, compiler, scopeOptions),
                    _ => throw new NotSupportedException()
                };
        }
    }
}
