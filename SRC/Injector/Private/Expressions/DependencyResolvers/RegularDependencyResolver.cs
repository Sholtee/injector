/********************************************************************************
* RegularDependencyResolver.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed class RegularDependencyResolver: IDependencyResolver
    {
        private static readonly MethodInfo
            FInjectorGet = MethodInfoExtractor.Extract<IInjector>(static i => i.Get(null!, null)),
            FInjectorTryGet = MethodInfoExtractor.Extract<IInjector>(static i => i.TryGet(null!, null));

        public object Id { get; } = nameof(RegularDependencyResolver);

        /// <summary>
        /// <code>(TInterface) injector.[Try]Get(typeof(TInterface), options?.Name)</code>
        /// </summary>
        public Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, object? context, CallNextDelegate<object?, Expression> next)
        {
            if (dependency.Type.IsClass || dependency.Type.IsInterface)
            {
                return Expression.Convert
                (
                    Expression.Call
                    (
                        injector,
                        dependency.Options?.Optional is true ? FInjectorTryGet : FInjectorGet,
                        Expression.Constant(dependency.Type),
                        Expression.Constant(dependency.Options?.Key, typeof(string))
                    ),
                    dependency.Type
                );
            }
            return next(context);
        }
    }
}
