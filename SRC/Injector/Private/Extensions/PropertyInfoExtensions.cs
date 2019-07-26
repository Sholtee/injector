﻿/********************************************************************************
* PropertyInfoExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    internal static class PropertyInfoExtensions
    {
        public static void FastSetValue(this PropertyInfo src, object instance, object value)
        {
            Action<object, object> setter = Cache<PropertyInfo, Action<object, object>>.GetOrAdd(src, () =>
            {
                ParameterExpression 
                    inst = Expression.Parameter(typeof(object), "instance"),
                    val  = Expression.Parameter(typeof(object), "value");

                return Expression.Lambda<Action<object, object>>
                (
                    Expression.Assign
                    (
                        Expression.Property(Expression.Convert(inst, src.ReflectedType), src), 
                        Expression.Convert(val, src.PropertyType)
                    ), 
                    inst, 
                    val
                )
                .Compile();
            });

            setter(instance, value);
        }
    }
}
