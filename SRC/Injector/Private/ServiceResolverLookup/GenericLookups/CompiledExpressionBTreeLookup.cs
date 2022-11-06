/********************************************************************************
* CompiledExpressionBTreeLookup.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed partial class CompiledExpressionBTreeLookup<TData> : ILookup<TData, CompiledExpressionBTreeLookup<TData>>
    {
        private readonly RedBlackTree<KeyValuePair<CompositeKey, TData>> FTree;

        private Func<CompositeKey, TData?>? FTryGet;

        public CompiledExpressionBTreeLookup(RedBlackTree<KeyValuePair<CompositeKey, TData>> tree, IDelegateCompiler compiler)
        {
            FTree = tree.Clone();
            Compiler = compiler;

            compiler.Compile
            (
                BuildTree(tree),
                builtDelegate => FTryGet = builtDelegate
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompiledExpressionBTreeLookup<TData> Add(CompositeKey key, TData data) => new
        (
            FTree.With
            (
                new KeyValuePair<CompositeKey, TData>(key, data)
            ),
            Compiler
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(CompositeKey key, TData data) => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(CompositeKey key, out TData data) => (data = FTryGet!(key)!) is not null;

        public IDelegateCompiler Compiler { get; set; }
    }
}
