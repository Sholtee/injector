/********************************************************************************
* CompiledBTreeLookup.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed partial class CompiledBTreeLookup: ILookup<CompositeKey, AbstractServiceEntry, CompiledBTreeLookup>
    {
        private TryGetEntry FTryGet;

        private bool FCompiled;

        private readonly RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> FTree;

        private readonly IDelegateCompiler FCompiler;

        private void Compile() => FCompiler.Compile
        (
            BuildTree(FTree),
            tryGet => FTryGet = tryGet
        );

        private CompiledBTreeLookup(RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> tree, IDelegateCompiler compiler)
        {
            FTree = tree;
            FTryGet = tree.TryGet;
            FCompiler = compiler;
        }

        public CompiledBTreeLookup(IDelegateCompiler compiler) : this(RedBlackTreeExtensions.CreateLookup<CompositeKey, AbstractServiceEntry>(), compiler)
        {
        }

        public bool Compiled
        {
            get => FCompiled;
            set
            {
                if (!value)
                    FTryGet = FTree.TryGet;
                else
                    Compile();

                FCompiled = value;
            }
        }

        /// <summary>
        /// Extends this tree into a new lookup
        /// </summary>
        public CompiledBTreeLookup With(CompositeKey key, AbstractServiceEntry data) => new
        (
            FTree.With
            (
                new KeyValuePair<CompositeKey, AbstractServiceEntry>(key, data)
            ),
            FCompiler
        )
        {
            Compiled = Compiled
        };

        public bool TryAdd(CompositeKey key, AbstractServiceEntry data)
        {
            bool updated = FTree.TryAdd(key, data);
            if (updated && FCompiled)
                Compile();
            return updated;
        }

        /// <summary>
        /// Tries to find an item in this lookup.
        /// </summary>
        /// <remarks>Until the first <see cref="Compile"/> call this implementation uses the underlying red-black tree to acquire the result.</remarks>
        public bool TryGet(CompositeKey key, out AbstractServiceEntry data) => FTryGet(key, out data);
    }
}
