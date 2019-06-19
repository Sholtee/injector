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

namespace Solti.Utils.DI.Internals
{
    internal static class MethodInfoExtensions
    {
        private static readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> FCache = new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>();

        public static Expression<TLambda> ToLambda<TLambda>(this MethodInfo method, Expression instance, Func<ParameterInfo, int, Expression> getArgument, params ParameterExpression[] parameters)
        {
            //
            // (target, paramz) => (Type_3) ((Type_0) target).Method((Type_1) paramz[0], (Type_2) paramz[1], ...)
            // ----------------------------------------------------------------------------------------------------------
            // (target, paramz) => {((Type_0) target).Method((Type_1) paramz[0], (Type_2) paramz[1], ...); return default(object);}
            //

            Expression call = Expression.Call
            (
                instance != null ? Expression.Convert(instance, method.ReflectedType) : null,
                method,
                method
                    .GetParameters()
                    .Select((param, i) => Expression.Convert(getArgument(param, i), param.ParameterType))
            );

            call = method.ReturnType != typeof(void)
                ? (Expression) Expression.Convert(call, typeof(object))
                : Expression.Block(typeof(object), call, Expression.Default(typeof(object)));

            return Expression.Lambda<TLambda>
            (
                call,    
                parameters
            );
        }

        public static Func<object, object[], object> ToDelegate(this MethodInfo method) => FCache.GetOrAdd(method, @void =>
        {
            ParameterExpression
                instance = Expression.Parameter(typeof(object),   nameof(instance)),
                paramz   = Expression.Parameter(typeof(object[]), nameof(paramz));

            return method.ToLambda<Func<object, object[], object>>
            (
                instance, 
                (param, i) => Expression.ArrayIndex(paramz, Expression.Constant(i)),
                instance,
                paramz
            ).Compile();
        });

        public static object Call(this MethodInfo method, object target, params object[] args) => method.ToDelegate()(target, args);
    }
}
