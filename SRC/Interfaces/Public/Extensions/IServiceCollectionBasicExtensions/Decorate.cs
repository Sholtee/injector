/********************************************************************************
* Decorate.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Hooks into the instantiating process of last registered service. Useful when you want to add additional functionality (e.g. parameter validation):
        /// <code>
        /// using Solti.Utils.Proxy.Generators;
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs
        ///         .Service&lt;IMyService, MyService&gt;()
        ///         .Decorate((scope, type, instance) => ProxyGenerator&lt;IMyService, ParameterValidatorInterceptor&lt;IMyService&gt;&gt;.Activate(Tuple.Create(instance))),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of decorator function.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Expression<DecoratorDelegate> decorator)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (decorator is null)
                throw new ArgumentNullException(nameof(decorator));

            self.Last().Decorate(decorator);
            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process of a registered service. Useful when you want to add additional functionality (e.g. parameter validation):
        /// <code>
        /// using Solti.Utils.Proxy.Generators;
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs
        ///         .Service&lt;IMyService, MyService&gt;("svcName")
        ///         .Decorate(typeof(IMyService), "svcName", (scope, type, instance) => ProxyGenerator&lt;IMyService, ParameterValidatorInterceptor&lt;IMyService&gt;&gt;.Activate(Tuple.Create(instance))),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <param name="key">The (optional) service key.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of decorator function.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type type, object? key, Expression<DecoratorDelegate> decorator)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (decorator is null)
                throw new ArgumentNullException(nameof(decorator));

            self.Find(type, key).Decorate(decorator);
            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process of a registered service. Useful when you want to add additional functionality (e.g. parameter validation):
        /// <code>
        /// using Solti.Utils.Proxy.Generators;
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs
        ///         .Service&lt;IMyService, MyService&gt;()
        ///         .Decorate(typeof(IMyService), (scope, type, instance) => ProxyGenerator&lt;IMyService, ParameterValidatorInterceptor&lt;IMyService&gt;&gt;.Activate(Tuple.Create(instance))),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of the decorator function.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type type, Expression<DecoratorDelegate> decorator) =>
            self.Decorate(type, null, decorator);

        /// <summary>
        /// Hooks into the instantiating process of a registered service. Useful when you want to add additional functionality (e.g. parameter validation):
        /// <code>
        /// using Solti.Utils.Proxy.Generators;
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs
        ///         .Service&lt;IMyService, MyService&gt;("svcName")
        ///         .Decorate&lt;IMyService&gt;("svcName", (scope, instance) => ProxyGenerator&lt;IMyService, ParameterValidatorInterceptor&lt;IMyService&gt;&gt;.Activate(Tuple.Create(instance))),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service name.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of the decorator function.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceCollection Decorate<TType>(this IServiceCollection self, object? key, Expression<DecoratorDelegate<TType>> decorator) where TType : class =>
            self.Decorate(typeof(TType), key, WrapToStandardDelegate(decorator));

        /// <summary>
        /// Hooks into the instantiating process of a registered service. Useful when you want to add additional functionality (e.g. parameter validation):
        /// <code>
        /// using Solti.Utils.Proxy.Generators;
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs
        ///         .Service&lt;IMyService, MyService&gt;()
        ///         .Decorate&lt;IMyService&gt;((scope, instance) => ProxyGenerator&lt;IMyService, ParameterValidatorInterceptor&lt;IMyService&gt;&gt;.Activate(Tuple.Create(instance))),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of the decorator function.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceCollection Decorate<TType>(this IServiceCollection self, Expression<DecoratorDelegate<TType>> decorator) where TType: class =>
            self.Decorate(typeof(TType), WrapToStandardDelegate(decorator));

        private sealed class ParameterReplacer : ExpressionVisitor
        {
            public ParameterExpression TargetParameter { get; }

            public Expression Replacement { get; }

            public ParameterReplacer(ParameterExpression targetParameter, ParameterExpression replacement)
            {
                TargetParameter = targetParameter;
                Replacement = Expression.Convert(replacement, TargetParameter.Type);
            }

            protected override Expression VisitParameter(ParameterExpression node) => node == TargetParameter
                ? Replacement
                : base.VisitParameter(node);
        }

        private static Expression<DecoratorDelegate> WrapToStandardDelegate<TType>(Expression<DecoratorDelegate<TType>> decorator) where TType : class
        {
            if (decorator is null)
                throw new ArgumentNullException(nameof(decorator));

            ParameterExpression
                injector = decorator.Parameters[0],
                type     = Expression.Parameter(typeof(Type), nameof(type)),
                instance = Expression.Parameter(typeof(object), nameof(instance));

            return Expression.Lambda<DecoratorDelegate>
            (
                new ParameterReplacer(decorator.Parameters[1], instance).Visit(decorator.Body),
                injector,
                type,
                instance
            );
        }
    }
}