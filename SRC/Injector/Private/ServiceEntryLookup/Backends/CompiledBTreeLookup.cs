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

    internal sealed partial class CompiledBTreeLookup: BTreeLookup
    {
        private Func<CompositeKey, AbstractServiceEntry?>? FTryGet;

        public CompiledBTreeLookup(RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> source, IDelegateCompiler compiler): base(source)
        {
            Compiler = compiler;

            compiler.Compile
            (
                BuildTree(FTree),
                builtDelegate => FTryGet = builtDelegate
            );
        }

        public override ILookup<CompositeKey, AbstractServiceEntry> Add(CompositeKey key, AbstractServiceEntry data) => new CompiledBTreeLookup
        (
            FTree.With
            (
                new KeyValuePair<CompositeKey, AbstractServiceEntry>(key, data)
            ),
            Compiler
        );

        public override bool TryAdd(CompositeKey key, AbstractServiceEntry data) => throw new NotSupportedException();

        public override bool TryGet(CompositeKey key, out AbstractServiceEntry data) => (data = FTryGet!(key)!) is not null;

        public IDelegateCompiler Compiler { get; }
    }
}
