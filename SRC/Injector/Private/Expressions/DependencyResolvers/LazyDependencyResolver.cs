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

    internal sealed class LazyDependencyResolver : RegularLazyDependencyResolver
    {
        protected override MethodInfo CreateLazy { get; } = MethodInfoExtractor.Extract(static () => CreateLazyImpl<object>(null!, null)).GetGenericMethodDefinition();

        protected override MethodInfo CreateLazyOpt { get; }= MethodInfoExtractor.Extract(static () => CreateLazyOptImpl<object>(null!, null)).GetGenericMethodDefinition();

        //
        // This methods are more performant since the factory function is static (there is no need for
        // instantiating a new lambda on each invocation).
        //

        private static ILazy<TService> CreateLazyImpl<TService>(IInjector injector, string? name) =>
            new LazyHavingContext<TService, Tuple<IInjector, string?>>
            (
                static ctx => (TService)ctx.Item1.Get(typeof(TService), ctx.Item2),
                Tuple.Create(injector, name)
            );

        private static ILazy<TService> CreateLazyOptImpl<TService>(IInjector injector, string? name) =>
            new LazyHavingContext<TService, Tuple<IInjector, string?>>
            (
                static ctx => (TService)ctx.Item1.TryGet(typeof(TService), ctx.Item2)!,
                Tuple.Create(injector, name)
            );

        public override Expression Resolve(ParameterExpression injector, DependencyDescriptor dependency, object? userData, Next<Expression> next)
        {
            Type? iface = ParseDependency(dependency, typeof(ILazy<>));
            if (iface is null)
                return next();

            return ResolveLazyService
            (
                injector,
                iface,
                dependency.Options
            );
        }
    }
}
