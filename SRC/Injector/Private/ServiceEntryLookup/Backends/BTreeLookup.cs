/********************************************************************************
* BTreeLookup.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal class BTreeLookup : ILookup<IServiceId, AbstractServiceEntry, BTreeLookup>
    {
        private static bool TryGet(RedBlackTreeNode<AbstractServiceEntry>? node, IServiceId key, out AbstractServiceEntry result)
        {
            if (node is null)
            {
                result = default!;
                return false;
            }

            int order = ServiceIdComparer.Instance.Compare(key, node.Data);

            if (order < 0)
                return TryGet(node.Left, key, out result);

            if (order > 0)
                return TryGet(node.Right, key, out result);

            result = node.Data;
            return true;
        }

        protected readonly RedBlackTree<AbstractServiceEntry> FTree;

        protected BTreeLookup(RedBlackTree<AbstractServiceEntry> tree)
            => FTree = tree;

        public BTreeLookup() : this
        (
            new RedBlackTree<AbstractServiceEntry>
            (
                ServiceIdComparer.Instance
            )
        ) {}

        /// <summary>
        /// Extends this tree into a new lookup
        /// </summary>
        public virtual BTreeLookup With(AbstractServiceEntry data) => new
        (
            FTree.With(data)
        );

        public virtual bool TryAdd(AbstractServiceEntry data) => FTree.Add(data);

        /// <summary>
        /// Tries to find an item in this lookup.
        /// </summary>
        public virtual bool TryGet(IServiceId key, out AbstractServiceEntry data) => TryGet(FTree.Root, key, out data);

        public int Count => FTree.Count;

        /// <summary>
        /// Returns the clone of the underlying tree.
        /// </summary>
        public RedBlackTree<AbstractServiceEntry> GetUnderlyingTree() => FTree.Clone();

        public BTreeLookup Compile(IDelegateCompiler compiler) => new CompiledBTreeLookup(this, compiler);
    }
}
