/********************************************************************************
* BTreeLookup.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed class BTreeLookup : ILookup<CompositeKey, AbstractServiceEntry, BTreeLookup>
    {
        private readonly RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> FTree;

        private BTreeLookup(RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> tree) => FTree = tree;

        public BTreeLookup() : this
        (
            RedBlackTreeExtensions.CreateLookup<CompositeKey, AbstractServiceEntry>()
        ) {}

        public BTreeLookup Add(CompositeKey key, AbstractServiceEntry data) => new
        (
            FTree.With
            (
                new KeyValuePair<CompositeKey, AbstractServiceEntry>(key, data)
            )
        );

        public bool TryAdd(CompositeKey key, AbstractServiceEntry data) => FTree.Add
        (
            new KeyValuePair<CompositeKey, AbstractServiceEntry>(key, data)
        );

        public bool TryGet(CompositeKey key, out AbstractServiceEntry data) => FTree.TryGet(key, out data);

        public CompiledBTreeLookup Compile(IDelegateCompiler compiler) => new(FTree.Clone(), compiler);
    }
}
