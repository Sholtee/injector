/********************************************************************************
* MethodInfoExtractor.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    internal static class MethodInfoExtractor
    {
        public static MethodInfo Extract<T>(Expression<Action<T>> expr) => ((MethodCallExpression) expr.Body).Method;
    }
}
