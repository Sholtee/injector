﻿/********************************************************************************
* ExplicitArgResolver_Dict.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ExplicitArgResolver_Dict : IDependencyResolver
    {
        public Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, Next<Expression> next)
        {
            if (userData is IReadOnlyDictionary<string, object?> explicitArgs && explicitArgs.TryGetValue(dependency.Name, out object? val))
            {
                if ((val is null && !dependency.Type.IsValueType) || (val is not null && dependency.Type.IsAssignableFrom(val.GetType())))
                {
                    return Expression.Convert(Expression.Constant(val, typeof(object)), dependency.Type);
                }
            }
            return next();
        }
    }
}
