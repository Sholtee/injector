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
    using Primitives.Patterns;

    internal class BTreeLookup : ILookup<IServiceId, AbstractServiceEntry, BTreeLookup>
    {
        private sealed class KvpComparer : Singleton<KvpComparer>, IComparer<KeyValuePair<IServiceId, AbstractServiceEntry>>
        {
            public int Compare(KeyValuePair<IServiceId, AbstractServiceEntry> x, KeyValuePair<IServiceId, AbstractServiceEntry> y)
                => ServiceIdComparer.Instance.Compare(x.Key, y.Key);
        }

        private static bool TryGet(RedBlackTreeNode<KeyValuePair<IServiceId, AbstractServiceEntry>>? node, IServiceId key, out AbstractServiceEntry result)
        {
            if (node is null)
            {
                result = default!;
                return false;
            }

            int order = ServiceIdComparer.Instance.Compare(key, node.Data.Key);

            if (order < 0)
                return TryGet(node.Left, key, out result);

            if (order > 0)
                return TryGet(node.Right, key, out result);

            result = node.Data.Value;
            return true;
        }

        protected readonly RedBlackTree<KeyValuePair<IServiceId, AbstractServiceEntry>> FTree;

        protected BTreeLookup(RedBlackTree<KeyValuePair<IServiceId, AbstractServiceEntry>> tree)
            => FTree = tree;

        public BTreeLookup() : this
        (
            new RedBlackTree<KeyValuePair<IServiceId, AbstractServiceEntry>>
            (
                KvpComparer.Instance
            )
        ) {}

        /// <summary>
        /// Extends this tree into a new lookup
        /// </summary>
        public virtual BTreeLookup With(IServiceId key, AbstractServiceEntry data) => new
        (
            FTree.With
            (
                new KeyValuePair<IServiceId, AbstractServiceEntry>(key, data)
            )
        );

        public virtual bool TryAdd(IServiceId key, AbstractServiceEntry data) => FTree.Add
        (
            new KeyValuePair<IServiceId, AbstractServiceEntry>(key, data)
        );

        /// <summary>
        /// Tries to find an item in this lookup.
        /// </summary>
        public virtual bool TryGet(IServiceId key, out AbstractServiceEntry data) => TryGet(FTree.Root, key, out data);

        public int Count => FTree.Count;

        /// <summary>
        /// Returns the clone of the underlying tree.
        /// </summary>
        public RedBlackTree<KeyValuePair<IServiceId, AbstractServiceEntry>> GetUnderlyingTree()
            => FTree.Clone();

        public BTreeLookup Compile(IDelegateCompiler compiler) => new CompiledBTreeLookup(this, compiler);
    }
}
