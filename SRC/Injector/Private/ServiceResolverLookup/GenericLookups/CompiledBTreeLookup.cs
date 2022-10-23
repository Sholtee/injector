﻿/********************************************************************************
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

    internal sealed partial class CompiledBTreeLookup<TData> : ILookup<TData, CompiledBTreeLookup<TData>>
    {
        private readonly RedBlackTree<KeyValuePair<CompositeKey, TData>> FTree;

        private Func<CompositeKey, TData?>? FTryGet;

        public CompiledBTreeLookup(RedBlackTree<KeyValuePair<CompositeKey, TData>> tree, IDelegateCompiler compiler)
        {
            FTree = tree.Clone();
            Compiler = compiler;

            compiler.Compile
            (
                BuildTree(tree),
                builtDelegate => FTryGet = builtDelegate
            );
        }

        public CompiledBTreeLookup<TData> Add(CompositeKey key, TData data) => new
        (
            FTree.With
            (
                new KeyValuePair<CompositeKey, TData>(key, data)
            ),
            Compiler
        );

        public bool TryAdd(CompositeKey key, TData data) => throw new NotSupportedException();

        public bool TryGet(CompositeKey key, out TData data) => (data = FTryGet!(key)!) is not null;

        public IDelegateCompiler Compiler { get; set; }
    }
}
