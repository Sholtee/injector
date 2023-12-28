/********************************************************************************
* LazyDependencyResolver.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal sealed class LazyDependencyResolver : RegularLazyDependencyResolver
    {
        protected override MethodInfo CreateLazy { get; } = MethodInfoExtractor.Extract(static () => CreateLazyImpl<object>(null!, null)).GetGenericMethodDefinition();

        protected override MethodInfo CreateLazyOpt { get; } = MethodInfoExtractor.Extract(static () => CreateLazyOptImpl<object>(null!, null)).GetGenericMethodDefinition();

        private sealed record LazyContext(IInjector Injector, object? Key);

        private static class Factories<TService>
        {
            //
            // Converting methods to delegates may take a while, so do it only once
            //

            public static readonly Func<LazyContext, TService> Factory =
                static ctx => (TService) ctx.Injector.Get(typeof(TService), ctx.Key);
            public static readonly Func<LazyContext, TService> FactoryOpt =
                static ctx => (TService) ctx.Injector.TryGet(typeof(TService), ctx.Key)!;
        }

        private static ILazy<TService> CreateLazyImpl<TService>(IInjector injector, object? key) =>
            new LazyHavingContext<TService, LazyContext>
            (
                Factories<TService>.Factory,
                new LazyContext(injector, key)
            );

        private static ILazy<TService> CreateLazyOptImpl<TService>(IInjector injector, object? key) =>
            new LazyHavingContext<TService, LazyContext>
            (
                Factories<TService>.FactoryOpt,
                new LazyContext(injector, key)
            );

        public override object Id { get; } = nameof(LazyDependencyResolver);

        public override Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, object? context, CallNextDelegate<object?, Expression> next)
        {
            Type? type = ParseDependency(dependency, typeof(ILazy<>));
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
