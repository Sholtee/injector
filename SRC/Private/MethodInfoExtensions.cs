﻿/********************************************************************************
* MethodInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI
{
    internal static class MethodInfoExtensions
    {
        private static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> FCache = new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>();

        public static bool MethodRegistered(MethodInfo method)
        {
            return FCache.ContainsKey(method);
        }

        public static object FastInvoke(this MethodInfo method, object target, params object[] args)
        {
            Func<object, object[], object> invoke = FCache.GetOrAdd(method, @void =>
            {
                ParameterExpression
                    instance = Expression.Parameter(typeof(object),   "instance"),
                    paramz   = Expression.Parameter(typeof(object[]), "paramz");

                Expression call =  Expression.Call
                (
                    Expression.Convert(instance, method.ReflectedType), 
                    method, 
                    method
                        .GetParameters()
                        .Select((para, i) => Expression.Convert(Expression.ArrayIndex(paramz, Expression.Constant(i)), para.ParameterType))
                );

                call = method.ReturnType != typeof(void)
                    ? (Expression) Expression.Convert(call, typeof(object))
                    : Expression.Block(typeof(object), call, Expression.Default(typeof(object)));
     
                return Expression.Lambda<Func<object, object[], object>>
                (
                    call,
                    instance, 
                    paramz
                ).Compile();
            });

            return invoke(target, args);
        }
    }
}
