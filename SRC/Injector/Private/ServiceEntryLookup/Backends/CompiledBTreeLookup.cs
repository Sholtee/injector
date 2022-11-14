/********************************************************************************
* CompiledBTreeLookup.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed partial class CompiledBTreeLookup: ILookup<CompositeKey, AbstractServiceEntry, CompiledBTreeLookup>
    {
        private /*readonly*/ Func<CompositeKey, AbstractServiceEntry?> FTryGet;

        private readonly RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> FTree;

        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
        public CompiledBTreeLookup(RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> source, IDelegateCompiler compiler)
        #pragma warning restore CS8618
        {
            FTree = source;
            Compiler = compiler;

            compiler.Compile
            (
                BuildTree(FTree),
                builtDelegate => FTryGet = builtDelegate
            );
        }

        public CompiledBTreeLookup Add(CompositeKey key, AbstractServiceEntry data) => new
        (
            FTree.With
            (
                new KeyValuePair<CompositeKey, AbstractServiceEntry>(key, data)
            ),
            Compiler
        );

        public bool TryAdd(CompositeKey key, AbstractServiceEntry data) => throw new NotSupportedException();

        public bool TryGet(CompositeKey key, out AbstractServiceEntry data) => (data = FTryGet(key)!) is not null;

        public IDelegateCompiler Compiler { get; set; }
    }
}
