/********************************************************************************
* MethodBaseExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    internal static class MethodBaseExtensions
    {
        public static Expression<TLambda> ToLambda<TLambda>(this MethodBase methodBase, Func<ParameterInfo, int, Expression> getArgument, params ParameterExpression[] parameters)
        {
            return Expression.Lambda<TLambda>
            (
                methodBase switch
                {
                    ConstructorInfo ctor => Expression.New(ctor, GetArguments()),
                    MethodInfo method => Expression.Call(method, GetArguments()),
                    _ => throw new NotSupportedException() // TODO
                },
                parameters
            );

            IEnumerable<UnaryExpression> GetArguments() => methodBase.GetParameters().Select((param, i) => Expression.Convert
            (
                getArgument(param, i),
                param.ParameterType
            ));
        }

        public static Func<object?[], object> ToDelegate(this MethodBase methodBase) => Cache.GetOrAdd(methodBase, () =>
        {
            ParameterExpression paramz = Expression.Parameter(typeof(object[]), nameof(paramz));

            return methodBase.ToLambda<Func<object?[], object>>
            (
                (param, i) => Expression.ArrayAccess(paramz, Expression.Constant(i)),
                paramz
            ).Compile();
        });

        public static object Call(this MethodBase methodBase, params object?[] args) => methodBase.ToDelegate().Invoke(args);
    }
}
