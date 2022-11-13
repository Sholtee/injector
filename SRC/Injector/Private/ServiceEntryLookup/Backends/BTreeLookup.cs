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

    internal class BTreeLookup : ILookup<CompositeKey, AbstractServiceEntry>
    {
        protected readonly RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> FTree;

        protected BTreeLookup(RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> tree) => FTree = tree;

        public BTreeLookup() : this
        (
            RedBlackTreeExtensions.CreateLookup<CompositeKey, AbstractServiceEntry>()
        ) {}

        public virtual ILookup<CompositeKey, AbstractServiceEntry> Add(CompositeKey key, AbstractServiceEntry data) => new BTreeLookup
        (
            FTree.With
            (
                new KeyValuePair<CompositeKey, AbstractServiceEntry>(key, data)
            )
        );

        public virtual bool TryAdd(CompositeKey key, AbstractServiceEntry data) => FTree.Add
        (
            new KeyValuePair<CompositeKey, AbstractServiceEntry>(key, data)
        );

        public virtual bool TryGet(CompositeKey key, out AbstractServiceEntry data) => FTree.TryGet(key, out data);

        public CompiledBTreeLookup Compile(IDelegateCompiler compiler) => new(FTree.Clone(), compiler);
    }
}
