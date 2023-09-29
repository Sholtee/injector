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
        ///         .Decorate((scope, iface, instance) => ProxyGenerator&lt;IMyService, ParameterValidatorInterceptor&lt;IMyService&gt;&gt;.Activate(Tuple.Create(instance))),
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
        ///         .Decorate(typeof(IMyService), "svcName", (scope, iface, instance) => ProxyGenerator&lt;IMyService, ParameterValidatorInterceptor&lt;IMyService&gt;&gt;.Activate(Tuple.Create(instance))),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <param name="name">The (optional) service name.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of decorator function.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type iface, object? name, Expression<DecoratorDelegate> decorator)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (decorator is null)
                throw new ArgumentNullException(nameof(decorator));

            self.Find(iface, name).Decorate(decorator);
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
        ///         .Decorate(typeof(IMyService), (scope, iface, instance) => ProxyGenerator&lt;IMyService, ParameterValidatorInterceptor&lt;IMyService&gt;&gt;.Activate(Tuple.Create(instance))),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of the decorator function.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type iface, Expression<DecoratorDelegate> decorator) =>
            self.Decorate(iface, null, decorator);

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
        /// <param name="name">The (optional) service name.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of the decorator function.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceCollection Decorate<TInterface>(this IServiceCollection self, object? name, Expression<DecoratorDelegate<TInterface>> decorator) where TInterface : class =>
            self.Decorate(typeof(TInterface), name, WrapToStandardDelegate(decorator));

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
        public static IServiceCollection Decorate<TInterface>(this IServiceCollection self, Expression<DecoratorDelegate<TInterface>> decorator) where TInterface: class =>
            self.Decorate(typeof(TInterface), WrapToStandardDelegate(decorator));

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

        private static Expression<DecoratorDelegate> WrapToStandardDelegate<TInterface>(Expression<DecoratorDelegate<TInterface>> decorator) where TInterface : class
        {
            if (decorator is null)
                throw new ArgumentNullException(nameof(decorator));

            ParameterExpression
                injector = decorator.Parameters[0],
                iface    = Expression.Parameter(typeof(Type), nameof(iface)),
                instance = Expression.Parameter(typeof(object), nameof(instance));

            return Expression.Lambda<DecoratorDelegate>
            (
                new ParameterReplacer(decorator.Parameters[1], instance).Visit(decorator.Body),
                injector,
                iface,
                instance
            );
        }
    }
}