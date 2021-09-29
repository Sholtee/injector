/********************************************************************************
* ArrayFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Primitives;

    internal static class ArrayFactory<T> where T : new()
    {
        public static Func<T[]> Create(int count)
        {
            Expression<Func<T[]>> expr = Expression.Lambda<Func<T[]>>
            (
                Expression.NewArrayInit(typeof(T), InitItems())
            );

            Debug.WriteLine($"Created array factory:{Environment.NewLine}{expr.GetDebugView()}");

            return expr.Compile();

            IEnumerable<NewExpression> InitItems()
            {
                ConstructorInfo ctor = typeof(T).GetConstructor(Type.EmptyTypes);

                for (int i = 0; i < count; i++)
                {
                    yield return Expression.New(ctor);
                }
            }
        }
    }
}
