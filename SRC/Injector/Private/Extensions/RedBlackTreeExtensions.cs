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
    using Primitives.Patterns;

    internal static class RedBlackTreeExtensions
    {
        private sealed class KVPComparer<TKey, TValue> : Singleton<KVPComparer<TKey, TValue>>, IComparer<KeyValuePair<TKey, TValue>> where TKey: IComparable<TKey>
        {
            public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => x.Key.CompareTo(y.Key);
        }

        private static bool TryGet<TKey, TValue>(RedBlackTreeNode<KeyValuePair<TKey, TValue>>? node, TKey key, out TValue result) where TKey : IComparable<TKey>
        {
            if (node is null)
            {
                result = default!;
                return false;
            }

            int order = key.CompareTo(node.Data.Key);

            if (order < 0)
                return TryGet(node.Left, key, out result);

            if (order > 0)
                return TryGet(node.Right, key, out result);

            result = node.Data.Value;
            return true;
        }

        public static RedBlackTree<KeyValuePair<TKey, TValue>> Create<TKey, TValue>() where TKey : IComparable<TKey> => new
        (
            KVPComparer<TKey, TValue>.Instance
        );

        public static bool TryGet<TKey, TValue>(this RedBlackTree<KeyValuePair<TKey, TValue>> src, TKey key, out TValue result) where TKey : IComparable<TKey> => TryGet
        (
            src.Root,
            key,
            out result
        );
    }
}
