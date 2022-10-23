﻿/********************************************************************************
* BTreeLookup.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

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

        public BTreeLookup<TData> Add(CompositeKey key, TData data) => new
        (
            FTree.With
            (
                new KeyValuePair<CompositeKey, TData>(key, data)
            )
        );

        public bool TryAdd(CompositeKey key, TData data) => FTree.Add
        (
            new KeyValuePair<CompositeKey, TData>(key, data)
        );

        public bool TryGet(CompositeKey key, out TData data) => FTree.TryGet(key, out data);

        public CompiledBTreeLookup<TData> Compile(IDelegateCompiler compiler) => new
        (
            FTree,
            compiler
        ); 
    }
}
