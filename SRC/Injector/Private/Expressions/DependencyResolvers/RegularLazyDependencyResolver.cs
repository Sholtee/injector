/********************************************************************************
* RegularLazyDependencyResolver.cs                                              *
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
    using Primitives;

    internal class RegularLazyDependencyResolver : IDependencyResolver
    {
        protected virtual MethodInfo CreateLazy { get; } = MethodInfoExtractor.Extract(static () => CreateLazyImpl<object>(null!, null)).GetGenericMethodDefinition();

        protected virtual MethodInfo CreateLazyOpt { get; } = MethodInfoExtractor.Extract(static () => CreateLazyOptImpl<object>(null!, null)).GetGenericMethodDefinition();

        public virtual object Id { get; } = nameof(RegularLazyDependencyResolver);

        private static Lazy<TService> CreateLazyImpl<TService>(IInjector injector, object? key) =>
            new(() => (TService) injector.Get(typeof(TService), key));

        private static Lazy<TService> CreateLazyOptImpl<TService>(IInjector injector, object? key) =>
            new(() => (TService) injector.TryGet(typeof(TService), key)!);

        internal Expression ResolveLazyService(ParameterExpression injector, Type type, OptionsAttribute? options)
        {
            //
            // According to tests, runtime built lambdas containing a nested function are ridiculously slow
            // (I suspect the nested lambda is instantiated by an Activator.CreateInstance call).
            //

            /*
            Type delegateType = typeof(Func<>).MakeGenericType(type);

            //
            // () => (type) injector.[Try]Get(type, key)
            //

            LambdaExpression valueFactory = Expression.Lambda
            (
                delegateType,
                next()
            );

            //
            // new Lazy<type>(() => (type) injector.[Try]Get(type, key))
            //

            Type lazyType = typeof(Lazy<>).MakeGenericType(type);

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
                ? CreateLazyOpt
                : CreateLazy;

            return Expression.Call
            (
                createLazy.MakeGenericMethod(type),
                injector,
                Expression.Constant(options?.Key, typeof(object))
            );
        }

        protected static Type? ParseDependency(DependencyDescriptor dependency, Type lazyType)
        {
            Type type = dependency.Type;
            return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == lazyType
                ? type.GetGenericArguments().Single()
                : null;
        }

        /// <summary>
        /// <code>new Lazy&lt;TInterface&gt;(() => (TInterface) injector.[Try]Get(typeof(TInterface), options?.Name))</code>
        /// </summary>
        public virtual Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, object? context, CallNextDelegate<object?, Expression> next)
        {
            Type? type = ParseDependency(dependency, typeof(Lazy<>));
            if (type is null)
                return next(context);

            return ResolveLazyService
            (
                injector,
                type,
                dependency.Options
            );
        }
    }
}
