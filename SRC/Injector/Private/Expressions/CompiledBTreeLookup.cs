/********************************************************************************
* CompiledBTreeLookup.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal partial class CompiledBTreeLookup
    {
        private static readonly MethodInfo FCompareTo = MethodInfoExtractor.Extract<CompositeKey>(ck => ck.CompareTo(null!));

        private delegate bool TryGetEntry(CompositeKey key, out AbstractServiceEntry entry);

        private static IEnumerable<Expression> BuildNode
        (
            RedBlackTreeNode<KeyValuePair<CompositeKey, AbstractServiceEntry>>? node,
            ParameterExpression key,
            ParameterExpression entry,
            ParameterExpression order,
            LabelTarget ret
        )
        {
            if (node is null)
            {
                yield return Expression.Return
                (
                    ret,
                    Expression.Constant(false)
                );
                yield break;
            }

            //
            // order = key.CompareTo(node.Data);
            //

            yield return Expression.Assign
            (
                order,
                Expression.Call
                (
                    key,
                    FCompareTo,
                    Expression.Constant(node.Data.Key)
                )
            );

            //
            // if (order < 0) {...}
            // if (order > 0) {...}
            //

            yield return Expression.IfThen
            (
                Expression.LessThan(order, Expression.Constant(0)),
                ifTrue: Expression.Block
                (
                    BuildNode(node.Left, key, entry, order, ret)
                )
            );

            yield return Expression.IfThen
            (
                Expression.GreaterThan(order, Expression.Constant(0)),
                ifTrue: Expression.Block
                (
                    BuildNode(node.Right, key, entry, order, ret)
                )
            );

            //
            // entry = ...;
            // return true;
            //

            yield return Expression.Assign
            (
                entry,
                Expression.Constant(node.Data.Value)
            );

            yield return Expression.Return
            (
                ret,
                Expression.Constant(true)
            );
        }

        private static Expression<TryGetEntry> BuildTree(RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> tree)
        {
            ParameterExpression
                key = Expression.Parameter(typeof(CompositeKey), nameof(key)),
                entry = Expression.Parameter(typeof(AbstractServiceEntry).MakeByRefType(), nameof(entry));

            LabelTarget ret = Expression.Label(typeof(bool), nameof(ret));

            LabelExpression returnFalse = Expression.Label(ret, Expression.Constant(false));

            Expression body;
            if (tree.Root is null)
                body = returnFalse;
            else
            {
                ParameterExpression order = Expression.Variable(typeof(int), nameof(order));
                List<Expression> block = new
                (
                    BuildNode(tree.Root, key, entry, order, ret)
                )
                {
                    returnFalse
                };
                body = Expression.Block(new[] { order }, block);
            }

            Expression<TryGetEntry> lambda = Expression.Lambda<TryGetEntry>(body, key, entry);

            WriteLine($"Created tree:{Environment.NewLine}{lambda.GetDebugView()}");

            return lambda;
        }
    }
}
