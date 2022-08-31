/********************************************************************************
* RedBlackTreeExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Primitives;

    internal static class RedBlackTreeExtensions
    {
        private sealed class KVPComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>>
        {
            public Func<TKey, TKey, int> Implementation { get; }

            public KVPComparer(Func<TKey, TKey, int> implementation) => Implementation = implementation;

            public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => Implementation(x.Key, y.Key);
        }

        private static bool TryGet<TKey, TValue>(RedBlackTreeNode<KeyValuePair<TKey, TValue>>? node, Func<TKey, TKey, int> compare, TKey key, out TValue result)
        {
            if (node is null)
            {
                result = default!;
                return false;
            }

            int order = compare
            (
                key,
                node.Data.Key
            );

            if (order < 0)
                return TryGet(node.Left, compare, key, out result);

            if (order > 0)
                return TryGet(node.Right, compare, key, out result);

            result = node.Data.Value;
            return true;
        }

        public static RedBlackTree<KeyValuePair<TKey, TValue>> Create<TKey, TValue>(Func<TKey, TKey, int> compare) => new
        (
            new KVPComparer<TKey, TValue>(compare)
        );

        public static bool TryGet<TKey, TValue>(this RedBlackTree<KeyValuePair<TKey, TValue>> src, Func<TKey, TKey, int> compare, TKey key, out TValue result) => TryGet
        (
            src.Root,
            compare,
            key,
            out result
        );
    }
}
