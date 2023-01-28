/********************************************************************************
* ExplicitArgResolver_Obj.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed class ExplicitArgResolver_Obj : Singleton<ExplicitArgResolver_Obj>, IDependencyResolver
    {
        public Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, OptionsAttribute? options, object? userData, Func<Expression> next)
        {
            if (userData is object explicitArgs)
            {
                PropertyInfo? prop = explicitArgs
                    .GetType()
                    .GetProperty(dependency.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                if (prop?.CanRead is true && dependency.Type.IsAssignableFrom(prop.PropertyType))
                {
                    //
                    // explicitArgs.argName_1 
                    //

                    return Expression.Property
                    (
                        Expression.Constant(explicitArgs, explicitArgs.GetType()),
                        prop
                    );
                }
            }
            return next();
        }
    }
}
