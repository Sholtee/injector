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

    internal sealed class BatchedDelegateCompiler : IDelegateCompiler
    {
        private readonly List<(Expression Expression, MethodInfo Method)> FCompilations = new();

        public void Compile<TDelegate>(Expression<TDelegate> expression, Action<TDelegate> completionCallback) =>
            FCompilations.Add((expression, completionCallback.Method));

        public void Compile()
        {
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
                                Expression.Constant(compilation.Method),
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
