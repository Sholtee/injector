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

    internal class RegularLazyDependencyResolver : IDependencyResolver
    {
        protected virtual MethodInfo CreateLazy { get; } = MethodInfoExtractor.Extract(static () => CreateLazyImpl<object>(null!, null)).GetGenericMethodDefinition();

        protected virtual MethodInfo CreateLazyOpt { get; } = MethodInfoExtractor.Extract(static () => CreateLazyOptImpl<object>(null!, null)).GetGenericMethodDefinition();

        private static Lazy<TService> CreateLazyImpl<TService>(IInjector injector, string? name) =>
            new Lazy<TService>(() => (TService) injector.Get(typeof(TService), name));

        private static Lazy<TService> CreateLazyOptImpl<TService>(IInjector injector, string? name) =>
            new Lazy<TService>(() => (TService)injector.TryGet(typeof(TService), name)!);

        internal Expression ResolveLazyService(ParameterExpression injector, Type iface, OptionsAttribute? options)
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
                ? CreateLazyOpt
                : CreateLazy;

            return Expression.Call
            (
                createLazy.MakeGenericMethod(iface),
                injector,
                Expression.Constant(options?.Name, typeof(string))
            );
        }

        protected static Type? ParseDependency(DependencyDescriptor dependency, Type lazyType)
        {
            Type type = dependency.Type;

            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == lazyType)
            {
                type = type.GetGenericArguments().Single();
                if (type.IsInterface)
                    return type;
            }

            return null;
        }

        /// <summary>
        /// <code>new Lazy&lt;TInterface&gt;(() => (TInterface) injector.[Try]Get(typeof(TInterface), options?.Name))</code>
        /// </summary>
        public virtual Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, object? context, Next<object?, Expression> next)
        {
            Type? iface = ParseDependency(dependency, typeof(Lazy<>));
            if (iface is null)
                return next(context);

            return ResolveLazyService
            (
                injector,
                iface,
                dependency.Options
            );
        }
    }
}
