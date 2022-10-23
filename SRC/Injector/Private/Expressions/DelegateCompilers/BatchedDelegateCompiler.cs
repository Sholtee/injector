/********************************************************************************
* BatchedDelegateCompiler.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    /// <summary>
    /// Collects compilations and builds them in a batch. This is to reduce the number of <see cref="LambdaExpression.Compile()"/> calls
    /// </summary>
    /// <remarks>Lambda compilation is a time consuming operation as it requires an <see cref="Assembly"/> to be built runtime.</remarks>
    internal sealed class BatchedDelegateCompiler : IDelegateCompiler
    {
        private readonly List<(LambdaExpression LambdaExpression, Expression Callback)> FCompilations = new();

        public void Compile<TDelegate>(Expression<TDelegate> lambdaExpression, Action<TDelegate> completionCallback) =>
            FCompilations.Add((lambdaExpression, Expression.Constant(completionCallback)));

        public void Compile()
        {
            if (FCompilations.Count is 0)
                return;

            Expression<Action> expr = Expression.Lambda<Action>
            (
                Expression.Block
                (
                    FCompilations.Select
                    (
                        compilation => Expression.Invoke
                        (
                            compilation.Callback,
                            compilation.LambdaExpression
                        )
                    )
                )
            );
            
            Debug.WriteLine($"Created batched compilation:{Environment.NewLine}{expr.GetDebugView()}");

            FCompilations.Clear();
            expr.Compile().Invoke();
        }
    }
}
