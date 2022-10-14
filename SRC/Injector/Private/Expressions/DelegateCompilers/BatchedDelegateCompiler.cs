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

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed class BatchedDelegateCompiler : IDelegateCompiler
    {
        private readonly List<(Expression Expression, Expression Callback)> FCompilations = new();

        public void Compile<TDelegate>(Expression<TDelegate> expression, Action<TDelegate> completionCallback) =>
            FCompilations.Add((expression, Expression.Constant(completionCallback)));

        public void Compile()
        {
            if (FCompilations.Count is 0)
                return;

            try
            {
                Expression<Action> expr = Expression.Lambda<Action>
                (
                    Expression.Block
                    (
                        FCompilations.Select
                        (
                            compilation => Expression.Invoke
                            (
                                compilation.Callback,
                                compilation.Expression
                            )
                        )
                    )
                );

                Debug.WriteLine($"Created batched compilation:{Environment.NewLine}{expr.GetDebugView()}");

                expr.Compile().Invoke();
            }
            finally
            {
                FCompilations.Clear();
            }
        }
    }
}
