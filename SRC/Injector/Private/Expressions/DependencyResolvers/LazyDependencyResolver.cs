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

        private static class Factories<TService>
        {
            //
            // Converting methods to delegates may take a while, so do it only once
            //

            public static readonly Func<(IInjector Injector, string? Name), TService> Factory =
                static ctx => (TService) ctx.Injector.Get(typeof(TService), ctx.Name);
            public static readonly Func<(IInjector Injector, string? Name), TService> FactoryOpt =
                static ctx => (TService) ctx.Injector.TryGet(typeof(TService), ctx.Name)!;
        }

        private static ILazy<TService> CreateLazyImpl<TService>(IInjector injector, string? name) =>
            new LazyHavingContext<TService, (IInjector, string?)>
            (
                Factories<TService>.Factory,
                (injector, name)
            );

        private static ILazy<TService> CreateLazyOptImpl<TService>(IInjector injector, string? name) =>
            new LazyHavingContext<TService, (IInjector, string?)>
            (
                Factories<TService>.FactoryOpt,
                (injector, name)
            );

        public override object Id { get; } = nameof(LazyDependencyResolver);

        public override Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, object? context, CallNextDelegate<object?, Expression> next)
        {
            Type? iface = ParseDependency(dependency, typeof(ILazy<>));
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
