﻿/********************************************************************************
* ConstructorInfoExtensions.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    internal static class ConstructorInfoExtensions
    {
        public static Expression<TLambda> ToLambda<TLambda>(this ConstructorInfo ctor, Func<ParameterInfo, int, Expression> getArgument, params ParameterExpression[] parameters) => Expression.Lambda<TLambda>
        (
            Expression.New
            (
                ctor, 
                ctor.GetParameters().Select((param, i) => Expression.Convert
                (
                    getArgument(param, i),
                    param.ParameterType
                ))
            ),
            parameters
        );

        public static Func<object[], object> ToDelegate(this ConstructorInfo ctor) => Cache.GetOrAdd(ctor, () =>
        {
            ParameterExpression paramz = Expression.Parameter(typeof(object[]), nameof(paramz));

            return ctor.ToLambda<Func<object[], object>>
            (
                (param, i) => Expression.ArrayAccess(paramz, Expression.Constant(i)),
                paramz
            ).Compile();
        });

        public static object Call(this ConstructorInfo ctor, params object[] args) => ctor.ToDelegate()(args);
    }
}
