/********************************************************************************
* ConstructorInfoExtensions.cs                                                  *
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
    internal static class ConstructorInfoExtensions
    {
        private static readonly ConcurrentDictionary<ConstructorInfo, Func<object[], object>> FCache = new ConcurrentDictionary<ConstructorInfo, Func<object[], object>>();

        public static Expression<TLambda> ToLambda<TLambda>(this ConstructorInfo ctor, Func<Type, int, Expression> getArgument, params ParameterExpression[] parameters) => Expression.Lambda<TLambda>
        (
            Expression.New
            (
                ctor, 
                ctor.GetParameters().Select((param, i) => Expression.Convert
                (
                    getArgument(param.ParameterType, i),
                    param.ParameterType
                ))
            ),
            parameters
        );

        public static Func<object[], object> ToDelegate(this ConstructorInfo ctor) => FCache.GetOrAdd(ctor, @void =>
        {
            ParameterExpression paramz = Expression.Parameter(typeof(object[]), "paramz");

            return ctor.ToLambda<Func<object[], object>>
            (
                (parameterType, i) => Expression.ArrayAccess(paramz, Expression.Constant(i)),
                paramz
            ).Compile();
        });

        public static object Call(this ConstructorInfo ctor, params object[] args) => ctor.ToDelegate()(args);
    }
}
