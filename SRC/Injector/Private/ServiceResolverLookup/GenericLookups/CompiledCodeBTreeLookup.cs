/********************************************************************************
* CompiledCodeBTreeLookup.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace Solti.Utils.DI.Internals
{
    using Primitives;

    internal sealed partial class CompiledCodeBTreeLookup<TData> : ILookup<TData, CompiledCodeBTreeLookup<TData>>, IRoslyCompilable
    {
        private readonly RedBlackTree<KeyValuePair<CompositeKey, TData>> FTree;

        private Func<CompositeKey, TData?>? FTryGet;

        public CompiledCodeBTreeLookup(RedBlackTree<KeyValuePair<CompositeKey, TData>> tree, Compiler compiler)
        {
            FTree = tree.Clone();
            compiler.Compile(this);
        }

        public void CompletionCallback(object? result) => FTryGet = (Func<CompositeKey, TData?>) result!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompiledCodeBTreeLookup<TData> Add(CompositeKey key, TData data) => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(CompositeKey key, TData data) => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(CompositeKey key, out TData data) => (data = FTryGet!(key)!) is not null;
    }
}
