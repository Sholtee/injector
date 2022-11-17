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
            // Slow method
            //

            return FTree.TryGet(key, out data);
        }
    }
}
