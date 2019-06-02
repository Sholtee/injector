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

        public static bool MethodRegistered(MethodInfo method) => FCache.ContainsKey(method); // TODO: remove

        public static TDelegate ToDelegate<TDelegate>(this MethodInfo method, Expression instance, Func<Type, int, Expression> getArgument, params ParameterExpression[] parameters)
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
                    .Select((param, i) => Expression.Convert(getArgument(param.ParameterType, i), param.ParameterType))
            );

            call = method.ReturnType != typeof(void)
                ? (Expression) Expression.Convert(call, typeof(object))
                : Expression.Block(typeof(object), call, Expression.Default(typeof(object)));

            return Expression.Lambda<TDelegate>
            (
                call,    
                parameters
            ).Compile();
        }

        public static object Call(this MethodInfo method, object target, params object[] args)
        {
            Func<object, object[], object> invoke = FCache.GetOrAdd(method, @void =>
            {
                ParameterExpression
                    instance = Expression.Parameter(typeof(object),   "instance"),
                    paramz   = Expression.Parameter(typeof(object[]), "paramz");

                return method.ToDelegate<Func<object, object[], object>>(
                    instance, 
                    (paramType, i) => Expression.ArrayIndex(paramz, Expression.Constant(i)),
                    instance,
                    paramz);
            });

            return invoke(target, args);
        }
    }
}
