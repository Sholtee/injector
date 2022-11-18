/********************************************************************************
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

    internal sealed partial class CompiledBTreeLookup: ILookup<CompositeKey, AbstractServiceEntry, CompiledBTreeLookup>
    {
        private Func<CompositeKey, AbstractServiceEntry?>? FTryGet;

        private readonly RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> FTree;

        private readonly IDelegateCompiler FCompiler;

        private CompiledBTreeLookup(RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> tree, IDelegateCompiler compiler)
        {
            FTree = tree;
            FCompiler = compiler;
        }

        public CompiledBTreeLookup(IDelegateCompiler compiler) : this(RedBlackTreeExtensions.CreateLookup<CompositeKey, AbstractServiceEntry>(), compiler)
        {
        }

        public void Compile() => FCompiler.Compile
        (
            BuildTree(FTree),
            tryGet => FTryGet = tryGet
        );

        /// <summary>
        /// Extends this tree into a new lookup
        /// </summary>
        /// <remarks>The returned lookup is always compiled.</remarks>
        public CompiledBTreeLookup With(CompositeKey key, AbstractServiceEntry data)
        {
            CompiledBTreeLookup newTree = new
            (
                FTree.With
                (
                    new KeyValuePair<CompositeKey, AbstractServiceEntry>(key, data)
                ),
                FCompiler
            );
            newTree.Compile();
            return newTree;
        }

        public bool TryAdd(CompositeKey key, AbstractServiceEntry data) => FTree.TryAdd(key, data);

        /// <summary>
        /// Tries to find an item in this lookup.
        /// </summary>
        /// <remarks>Until the first <see cref="Compile"/> this implementation uses the underlying red-black tree to acquire the result.</remarks>
        public bool TryGet(CompositeKey key, out AbstractServiceEntry data)
        {
            //
            // Try the fast way if possible
            //

            if (FTryGet is not null)
            {
                data = FTryGet(key)!;
                return data is not null;
            }

            //
            // Not won... lets get slow
            //

            return FTree.TryGet(key, out data);
        }
    }
}
