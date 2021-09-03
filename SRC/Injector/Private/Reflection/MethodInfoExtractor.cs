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
        public static MethodInfo Extract(LambdaExpression expr) => ((MethodCallExpression) expr.Body).Method;

        public static MethodInfo Extract(Expression<Action> expr) => Extract((LambdaExpression) expr);

        public static MethodInfo Extract<T>(Expression<Action<T>> expr) => Extract((LambdaExpression) expr);
    }
}
