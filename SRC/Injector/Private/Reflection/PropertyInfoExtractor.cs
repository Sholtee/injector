/********************************************************************************
* PropertyInfoExtractor.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    internal static class PropertyInfoExtractor
    {
        public static PropertyInfo Extract(LambdaExpression expr) => (PropertyInfo) ((MemberExpression) expr.Body).Member;

        public static PropertyInfo Extract<TTarget, TProp>(Expression<Func<TTarget, TProp>> expr) => Extract((LambdaExpression) expr);

        public static PropertyInfo Extract<TProp>(Expression<Func<TProp>> expr) => Extract((LambdaExpression) expr);
    }
}
