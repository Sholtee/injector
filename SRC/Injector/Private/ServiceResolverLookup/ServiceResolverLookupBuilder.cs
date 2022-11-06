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

        public static IServiceResolverLookup Build(IReadOnlyCollection<AbstractServiceEntry> entries, ScopeOptions scopeOptions)
        {
            #pragma warning disable CA1304 // Specify CultureInfo
            switch (scopeOptions.Engine?.ToUpper() ?? (entries.Count <= BTREE_ITEM_THRESHOLD ? BTREE : DICT))
            #pragma warning restore CA1304 // Specify CultureInfo
            {
                case DICT:
                {
                    BatchedDelegateCompiler batchedDelegateCompiler = new();

                    var lookup = CreateInitialLookup<DictionaryLookup<ServiceResolver>, DictionaryLookup<AbstractServiceEntry>>(batchedDelegateCompiler);

                    batchedDelegateCompiler.Compile();

                    return lookup.ChangeBackend
                    (
                        static dict => dict,
                        static dict => dict,
                        CreateGraphBuilderFactory(SimpleDelegateCompiler.Instance)
                    );
                }

                case BTREE:
                {
                    BatchedDelegateCompiler batchedDelegateCompiler = new();

                    var lookup = CreateInitialLookup<BTreeLookup<ServiceResolver>, BTreeLookup<AbstractServiceEntry>>(batchedDelegateCompiler)
                        //
                        // Compile btrees in this batch
                        //
                        .ChangeBackend(btree => btree.Compile(batchedDelegateCompiler), btree => btree.Compile(batchedDelegateCompiler));

                    batchedDelegateCompiler.Compile();

                    return lookup.ChangeBackend
                    (
                        static btree => { btree.Compiler = SimpleDelegateCompiler.Instance; return btree; },
                        static btree => { btree.Compiler = SimpleDelegateCompiler.Instance; return btree; },
                        CreateGraphBuilderFactory(SimpleDelegateCompiler.Instance)
                    );
                }

                default: throw new NotSupportedException();
            }

            ConcurrentServiceResolverLookup<TResolverLookup, TEntryLookup> CreateInitialLookup<TResolverLookup, TEntryLookup>(IDelegateCompiler compiler)
                where TResolverLookup : class, ILookup<ServiceResolver, TResolverLookup>, new()
                where TEntryLookup : class, ILookup<AbstractServiceEntry, TEntryLookup>, new()
            =>
                new ConstructableConcurrentServiceResolverLookup<TResolverLookup, TEntryLookup>
                (
                    entries,
                    CreateGraphBuilderFactory(compiler)
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
