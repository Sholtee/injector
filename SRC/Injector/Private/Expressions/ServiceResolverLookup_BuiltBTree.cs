﻿/********************************************************************************
* ServiceResolverLookup_BuiltBTree.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Primitives;

    internal sealed partial class ServiceResolverLookup_BuiltBTree : ServiceResolverLookup_BTree
    {
        private static IEnumerable<Expression> Build<TResult>(RedBlackTreeNode<KeyValuePair<CompositeKey, TResult>>? node, ParameterExpression key, ParameterExpression order, LabelTarget ret)
        {
            if (node is null)
            {
                yield return Expression.Return
                (
                    ret,
                    Expression.Default(typeof(TResult))
                );
                yield break;
            }

            //
            // order = CompareServiceIds(iface, name, RelatedEntry.Interface, RelatedEntry.Name);
            //

            yield return Expression.Assign
            (
                order,
                Expression.Invoke
                (
                    Expression.Constant((Func<CompositeKey, CompositeKey, int>) CompareServiceIds),
                    key,
                    Expression.Constant(node.Data.Key)
                )
            );

            //
            // if (order is null) return Result;
            //

            yield return Expression.IfThen
            (
                Expression.Equal(order, Expression.Constant(0)),
                Expression.Return
                (
                    ret,
                    Expression.Constant(node.Data.Value, typeof(TResult))
                )
            );

            //
            // if (order < 0) {...}
            // else {...}
            //

            yield return Expression.IfThenElse
            (
                Expression.LessThan(order, Expression.Constant(0)),
                ifTrue: Expression.Block
                (
                    Build(node.Left, key, order, ret)
                ),
                ifFalse: Expression.Block
                (
                    Build(node.Right, key, order, ret)
                )
            );
        }

        private static Func<CompositeKey, TResult?> BuildSwitch<TResult>(RedBlackTree<KeyValuePair<CompositeKey, TResult>> tree)
        {
            ParameterExpression key = Expression.Parameter(typeof(CompositeKey), nameof(key));

            LabelTarget ret = Expression.Label(typeof(TResult), nameof(ret));

            LabelExpression returnNull = Expression.Label(ret, Expression.Default(ret.Type));

            Expression body;
            if (tree.Root is null)
                body = returnNull;
            else
            {
                ParameterExpression order = Expression.Variable(typeof(int), nameof(order));
                List<Expression> block = new
                (
                    Build(tree.Root, key, order, ret)
                )
                {
                    returnNull
                };
                body = Expression.Block(new[] { order }, block);
            }

            Expression<Func<CompositeKey, TResult?>> resolver = Expression.Lambda<Func<CompositeKey, TResult?>>(body,key);

            WriteLine($"Created resolver:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver.Compile();
        }
    }
}
