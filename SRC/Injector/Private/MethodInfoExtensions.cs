/********************************************************************************
* MethodInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace Solti.Utils.DI
{
    internal static class MethodInfoExtensions
    {
        private static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> FInvocation = new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>(); 

        public static object FastInvoke([NotNull] this MethodInfo method, [NotNull] object target, [NotNull] params object[] args)
        {
            Func<object, object[], object> invoke = FInvocation.GetOrAdd(method, @void =>
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
