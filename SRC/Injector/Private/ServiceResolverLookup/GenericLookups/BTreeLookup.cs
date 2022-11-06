/********************************************************************************
* BTreeLookup.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed class BTreeLookup<TData> : ILookup<TData, BTreeLookup<TData>>
    {
        private readonly RedBlackTree<KeyValuePair<CompositeKey, TData>> FTree;

        private BTreeLookup(RedBlackTree<KeyValuePair<CompositeKey, TData>> tree) => FTree = tree;

        public BTreeLookup() : this
        (
            RedBlackTreeExtensions.Create<CompositeKey, TData>()
        ) {}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BTreeLookup<TData> Add(CompositeKey key, TData data) => new
        (
            FTree.With
            (
                new KeyValuePair<CompositeKey, TData>(key, data)
            )
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(CompositeKey key, TData data) => FTree.Add
        (
            new KeyValuePair<CompositeKey, TData>(key, data)
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(CompositeKey key, out TData data) => FTree.TryGet(key, out data);

        public CompiledExpressionBTreeLookup<TData> Compile(IDelegateCompiler compiler) => new
        (
            FTree,
            compiler
        );

        public CompiledCodeBTreeLookup<TData> Compile(RoslynCompiler compiler) => new
        (
            FTree,
            compiler
        );
    }
}
