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
            IServiceEntryLookup result;
            BatchedDelegateCompiler delegateCompiler = new();
            delegateCompiler.BeginBatch();

            #pragma warning disable CA1304 // Specify CultureInfo
            switch (scopeOptions.Engine?.ToUpper() ?? (entries.Count <= BTREE_ITEM_THRESHOLD ? BTREE : DICT))
            #pragma warning restore CA1304 // Specify CultureInfo
            {
                case DICT:
                {
                    result = new ServiceEntryLookup<DictionaryLookup>
                    (
                        entries,
                        static () => new DictionaryLookup(),
                        CreateGraphBuilder
                    );
                    break;
                }

                case BTREE:
                {
                    ServiceEntryLookup<CompiledBTreeLookup> lookup = new
                    (
                        entries,
                        () => new CompiledBTreeLookup(delegateCompiler),
                        CreateGraphBuilder
                    );

                    lookup.EntryLookup.Compile();
                    lookup.GenericEntryLookup.Compile();

                    result = lookup;
                    break;
                }

                default: throw new NotSupportedException();
            }

            delegateCompiler.Compile();
            return result;

            IGraphBuilder CreateGraphBuilder(IServiceEntryLookup lookup) => scopeOptions.ServiceResolutionMode switch
            {
                ServiceResolutionMode.JIT => new ShallowDependencyGraphBuilder(delegateCompiler, lookup),
                ServiceResolutionMode.AOT => new RecursiveDependencyGraphBuilder(delegateCompiler, lookup, scopeOptions),
                _ => throw new NotSupportedException()
            };
        }
    }
}
