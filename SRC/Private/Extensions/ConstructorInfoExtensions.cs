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
        private static readonly ConcurrentDictionary<ConstructorInfo, Func<object[], object>> Factories = new ConcurrentDictionary<ConstructorInfo, Func<object[], object>>();

        public static TDelegate ToDelegate<TDelegate>(this ConstructorInfo ctor, Func<int, Type, Expression> getArgumentExpression, params ParameterExpression[] parameters) => Expression.Lambda<TDelegate>
        (
            Expression.New
            (
                ctor, 
                ctor.GetParameters().Select((para, i) => Expression.Convert
                (
                    getArgumentExpression(i, para.ParameterType),
                    para.ParameterType
                ))
            ),
            parameters
        ).Compile();

        public static object Call(this ConstructorInfo ctor, params object[] args)
        {
            Func<object[], object> factory = Factories.GetOrAdd(ctor, @void =>
            {
                ParameterExpression paramz = Expression.Parameter(typeof(object[]), "paramz");

                return ctor.ToDelegate<Func<object[], object>>(
                    (i, parameterType) => Expression.ArrayAccess(paramz, Expression.Constant(i)),
                    paramz);
            });
            return factory(args);
        }
    }
}
