/********************************************************************************
* ExpressionChain.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    [Ignore]
    [MemoryDiagnoser]
    public class ExpressionChain
    {
        private readonly Func<int, int> Fn;

        public ExpressionChain()
        {
            Expression<Func<int, int>> expr = i => i + 1;

            for (int i = 0; i < 4; i++)
            {
                ParameterExpression p = Expression.Parameter(typeof(int));
                expr = Expression.Lambda<Func<int, int>>(Expression.Invoke(expr, Expression.Add(p, Expression.Constant(1))), p);
            }

            Fn = expr.Compile();
        }

        [Benchmark]
        public void Invoke() => Fn(0);
    }
}
