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
            FInjectorGet = MethodInfoExtractor.Extract<IInjector>(static i => i.Get(null!, null)),
            FInjectorTryGet = MethodInfoExtractor.Extract<IInjector>(static i => i.TryGet(null!, null));

        /// <summary>
        /// <code>(TInterface) injector.[Try]Get(typeof(TInterface), options?.Name)</code>
        /// </summary>
        public Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, Func<Expression> next)
        {
            if (dependency.Type.IsInterface)
            {
                return Expression.Convert
                (
                    Expression.Call
                    (
                        injector,
                        dependency.Options?.Optional is true ? FInjectorTryGet : FInjectorGet,
                        Expression.Constant(dependency.Type),
                        Expression.Constant(dependency.Options?.Name, typeof(string))
                    ),
                    dependency.Type
                );
            }
            return next();
        }
    }
}
