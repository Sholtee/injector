/********************************************************************************
* CompiledBTreeLookup.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed partial class CompiledBTreeLookup: BTreeLookup
    {
        private TryGetEntry? FTryGet;

        private readonly IDelegateCompiler FCompiler;

        private CompiledBTreeLookup(RedBlackTree<AbstractServiceEntry> tree, IDelegateCompiler compiler) : base(tree)
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

        public override BTreeLookup With(AbstractServiceEntry data) => new CompiledBTreeLookup
        (
            FTree.With(data),
            FCompiler
        );

        public override bool TryAdd(AbstractServiceEntry data) => throw new NotSupportedException();

        public override bool TryGet(IServiceId key, out AbstractServiceEntry data) => FTryGet!(key, out data);
    }
}
