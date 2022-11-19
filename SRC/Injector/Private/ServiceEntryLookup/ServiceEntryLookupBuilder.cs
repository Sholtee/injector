/********************************************************************************
* ServiceEntryLookupBuilder.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class ServiceEntryLookupBuilder
    {
        //
        // According to performance tests, up to ~50 items built btree is faster than dictionary.
        // Assuming that there won't be more than 20 constructed generic service 30 seems a good
        // threshold.
        //

        public const int BTREE_ITEM_THRESHOLD = 30;

        public const string BTREE = nameof(BTREE);

        public const string DICT = nameof(DICT);

        public static IServiceEntryLookup Build(IReadOnlyCollection<AbstractServiceEntry> entries, ScopeOptions scopeOptions)
        {
            BatchedDelegateCompiler delegateCompiler = new();
            delegateCompiler.BeginBatch();

            #pragma warning disable CA1304 // Specify CultureInfo
            IServiceEntryLookup result = (scopeOptions.Engine?.ToUpper() ?? (entries.Count <= BTREE_ITEM_THRESHOLD ? BTREE : DICT)) switch
            #pragma warning restore CA1304 // Specify CultureInfo
            {
                DICT => new ServiceEntryLookup<DictionaryLookup>
                (
                    entries,
                    delegateCompiler,
                    backendFactory: static () => new DictionaryLookup(),
                    graphBuilderFactory: CreateGraphBuilder
                ),
                BTREE => new ServiceEntryLookup<CompiledBTreeLookup>
                (
                    entries,
                    delegateCompiler,
                    backendFactory: () => new CompiledBTreeLookup(delegateCompiler),
                    graphBuilderFactory: CreateGraphBuilder,
                    afterConstruction: static backend => backend.Compile()
                ),
                _ => throw new NotSupportedException()
            };

            delegateCompiler.Compile();
            return result;

            IGraphBuilder CreateGraphBuilder<TBackend>(ServiceEntryLookup<TBackend> lookup) where TBackend : class, ILookup<CompositeKey, AbstractServiceEntry, TBackend> => scopeOptions.ServiceResolutionMode switch
            {
                ServiceResolutionMode.JIT => new ShallowDependencyGraphBuilder(lookup),
                ServiceResolutionMode.AOT => new RecursiveDependencyGraphBuilder(lookup, lookup, scopeOptions),
                _ => throw new NotSupportedException()
            };
        }
    }
}
