/********************************************************************************
* ExplicitArgResolver_Obj.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ExplicitArgResolver_Obj : IDependencyResolver
    {
        public object Id { get; } = nameof(ExplicitArgResolver_Obj);

        public Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, object? context, NextDelegate<object?, Expression> next)
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
            return next(context);
        }
    }
}
