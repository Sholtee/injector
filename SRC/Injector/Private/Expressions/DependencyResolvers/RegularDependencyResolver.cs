/********************************************************************************
* RegularDependencyResolver.cs                                                  *
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

    internal sealed class RegularDependencyResolver: Singleton<RegularDependencyResolver>, IDependencyResolver
    {
        private static readonly MethodInfo
            FInjectorGet = MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null)),
            FInjectorTryGet = MethodInfoExtractor.Extract<IInjector>(i => i.TryGet(null!, null));

        /// <summary>
        /// <code>(TInterface) injector.[Try]Get(typeof(TInterface), options?.Name)</code>
        /// </summary>
        public Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, OptionsAttribute? options, object? userData, Func<Expression> next)
        {
            if (dependency.Type.IsInterface)
            {
                return Expression.Convert
                (
                    Expression.Call
                    (
                        injector,
                        options?.Optional is true ? FInjectorTryGet : FInjectorGet,
                        Expression.Constant(dependency.Type),
                        Expression.Constant(options?.Name, typeof(string))
                    ),
                    dependency.Type
                );
            }
            return next();
        }
    }
}
