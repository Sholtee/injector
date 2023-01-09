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
        private TryGetEntry? FTryGet;

        private readonly IDelegateCompiler FCompiler;

        private CompiledBTreeLookup(RedBlackTree<KeyValuePair<IServiceId, AbstractServiceEntry>> tree, IDelegateCompiler compiler) : base(tree)
        {
            FCompiler = compiler;
            FCompiler.Compile
            (
                BuildTree(FTree),
                tryGet => FTryGet = tryGet
            );
        }

        public CompiledBTreeLookup(BTreeLookup src, IDelegateCompiler compiler): this(src.GetUnderlyingTree(), compiler)
        {
        }

        public override BTreeLookup With(IServiceId key, AbstractServiceEntry data) => new CompiledBTreeLookup
        (
            FTree.With
            (
                new KeyValuePair<IServiceId, AbstractServiceEntry>(key, data)
            ),
            FCompiler
        );

        public override bool TryAdd(IServiceId key, AbstractServiceEntry data) => throw new NotSupportedException();

        public override bool TryGet(IServiceId key, out AbstractServiceEntry data) => FTryGet!(key, out data);
    }
}
