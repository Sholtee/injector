﻿/********************************************************************************
* BuiltBTreeLookup.cs                                                           *
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

        private static IEnumerable<Expression> BuildNode(RedBlackTreeNode<KeyValuePair<CompositeKey, AbstractServiceEntry>>? node, ParameterExpression key, ParameterExpression order, LabelTarget ret)
        {
            if (node is null)
            {
                yield return Expression.Return
                (
                    ret,
                    Expression.Default(typeof(AbstractServiceEntry))
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
                    BuildNode(node.Left, key, order, ret)
                )
            );

            yield return Expression.IfThen
            (
                Expression.GreaterThan(order, Expression.Constant(0)),
                ifTrue: Expression.Block
                (
                    BuildNode(node.Right, key, order, ret)
                )
            );

            //
            // return Result;
            //

            yield return Expression.Return
            (
                ret,
                Expression.Constant(node.Data.Value, typeof(AbstractServiceEntry))
            );
        }

        private static Expression<Func<CompositeKey, AbstractServiceEntry?>> BuildTree(RedBlackTree<KeyValuePair<CompositeKey, AbstractServiceEntry>> tree)
        {
            ParameterExpression key = Expression.Parameter(typeof(CompositeKey), nameof(key));

            LabelTarget ret = Expression.Label(typeof(AbstractServiceEntry), nameof(ret));

            LabelExpression returnNull = Expression.Label(ret, Expression.Default(ret.Type));

            Expression body;
            if (tree.Root is null)
                body = returnNull;
            else
            {
                ParameterExpression order = Expression.Variable(typeof(int), nameof(order));
                List<Expression> block = new
                (
                    BuildNode(tree.Root, key, order, ret)
                )
                {
                    returnNull
                };
                body = Expression.Block(new[] { order }, block);
            }

            Expression<Func<CompositeKey, AbstractServiceEntry?>> lambda = Expression.Lambda<Func<CompositeKey, AbstractServiceEntry?>>(body,key);

            WriteLine($"Created tree:{Environment.NewLine}{lambda.GetDebugView()}");

            return lambda;
        }
    }
}
