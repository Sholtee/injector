/********************************************************************************
* LazyDependencyResolver.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal sealed class LazyDependencyResolver: Singleton<LazyDependencyResolver>, IDependencyResolver
    {
        private static readonly MethodInfo
            FCreateLazy    = MethodInfoExtractor.Extract(static () => CreateLazy<object>(null!, null)).GetGenericMethodDefinition(),
            FCreateLazyOpt = MethodInfoExtractor.Extract(static () => CreateLazyOpt<object>(null!, null)).GetGenericMethodDefinition();

        private static Lazy<TService> CreateLazy<TService>(IInjector injector, string? name) => new Lazy<TService>(() => (TService) injector.Get(typeof(TService), name));

        private static Lazy<TService> CreateLazyOpt<TService>(IInjector injector, string? name) => new Lazy<TService>(() => (TService) injector.TryGet(typeof(TService), name)!);

        internal static Expression ResolveLazyService(ParameterExpression injector, Type iface, OptionsAttribute? options)
        {
            //
            // According to tests, runtime built lambdas containing a nested function are ridiculously slow
            // (I suspect the nested lambda is instantiated by an Activator.CreateInstance call).
            //

            /*
            Type delegateType = typeof(Func<>).MakeGenericType(iface);

            //
            // () => (iface) injector.[Try]Get(iface, svcName)
            //

            LambdaExpression valueFactory = Expression.Lambda
            (
                delegateType,
                next()
            );

            //
            // new Lazy<iface>(() => (iface) injector.[Try]Get(iface, svcName))
            //

            Type lazyType = typeof(Lazy<>).MakeGenericType(iface);

            return Expression.New
            (
                lazyType.GetConstructor(new[] { delegateType }) ?? throw new MissingMethodException(lazyType.Name, ConstructorInfo.ConstructorName),
                valueFactory
            );
            */

            //
            // This workaround solves the above mentioned issue but suppresses the ServiceRequestReplacerVisitor.
            // Altough it shouldn't matter as Lazy pattern is for services having considerable instatiation time.
            //

            MethodInfo createLazy = options?.Optional is true
                ? FCreateLazyOpt
                : FCreateLazy;

            return Expression.Call
            (
                createLazy.MakeGenericMethod(iface),
                injector,
                Expression.Constant(options?.Name, typeof(string))
            );
        }

        /// <summary>
        /// <code>new Lazy&lt;TInterface&gt;(() => (TInterface) injector.[Try]Get(typeof(TInterface), options?.Name))</code>
        /// </summary>
        public Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, Next<Expression> next)
        {
            Type type = dependency.Type;
            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                type = type.GetGenericArguments().Single();
                if (type.IsInterface)
                {
                    return ResolveLazyService(injector, type, dependency.Options);
                }
            }
            return next();
        }
    }
}
