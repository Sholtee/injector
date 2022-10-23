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
            ServiceResolverLookup initialLookup = (scopeOptions.Engine?.ToUpper() ?? (entries.Count <= BTREE_ITEM_THRESHOLD ? BTREE : DICT)) switch
            #pragma warning restore CA1304 // Specify CultureInfo
            {
                DICT => new ServiceResolverLookup
                (
                    entries,
                    new DictionaryLookup<ServiceResolver>(),
                    new DictionaryLookup<AbstractServiceEntry>(),
                    CreateGraphBuilder
                ),
                BTREE => new ServiceResolverLookup
                (
                    entries,
                    new BTreeLookup<ServiceResolver>(),
                    new BTreeLookup<AbstractServiceEntry>(),
                    CreateGraphBuilder
                ),
                _ => throw new NotSupportedException()
            };

            //
            // Iterate through all the regular entries to enforce them to be built.
            //

            foreach (AbstractServiceEntry entry in entries)
            {
                if (!entry.Interface.IsGenericTypeDefinition && !entry.State.HasFlag(ServiceEntryStates.Built))
                    _ = initialLookup.Get(entry.Interface, entry.Name);
            }

            //
            // Now turn the initial lookup to a thread safe one.
            //



            //
            // Lambda compilations are deferred, do it finally
            //

            delegateCompiler.Compile();

            IGraphBuilder CreateGraphBuilder(IServiceResolverLookup lookup) => scopeOptions.ServiceResolutionMode switch
            {
                ServiceResolutionMode.JIT => new ShallowDependencyGraphBuilder(delegateCompiler),
                ServiceResolutionMode.AOT => new RecursiveDependencyGraphBuilder(lookup, delegateCompiler, scopeOptions),
                _ => throw new NotSupportedException()
            };
        }
    }
}
