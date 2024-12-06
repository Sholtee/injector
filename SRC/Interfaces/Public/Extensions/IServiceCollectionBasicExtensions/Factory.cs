/********************************************************************************
* Factory.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Interfaces
{
    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Factory(typeof(IMyService), "serviceName", (scope, type) => new MyService(...), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once (with the given <paramref name="key"/>).</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="factoryExpr">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter. Note that the second parameter of the <paramref name="factoryExpr"/> is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You can register generic services (where the <paramref name="type"/> parameter is an open generic type).</remarks>
        [OverloadResolutionPriority(1)]
        public static IServiceCollection Factory(this IServiceCollection self, Type type, object? key, Expression<FactoryDelegate> factoryExpr, LifetimeBase lifetime, ServiceOptions? options = null)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (factoryExpr is null)
                throw new ArgumentNullException(nameof(factoryExpr));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            return self.Register
            (
                //
                // Further validations are done by the created xXxServiceEntry
                //

                lifetime.CreateFrom(type, key, factoryExpr, options ?? ServiceOptions.Default)
            );
        }

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Factory(typeof(IMyService), "serviceName", (scope, type) => new MyService(...), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once (with the given <paramref name="key"/>).</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter. Note that the second parameter of the <paramref name="factory"/> is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can register generic services (where the <paramref name="type"/> parameter is an open generic type).</item>
        /// <item>It's advised to utilize <paramref name="factory"/> expressions (using the <see cref="Factory(IServiceCollection, Type, object?, Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> method) insted of real factories as they are more performant.</item>
        /// </list>
        /// </remarks>
        public static IServiceCollection Factory(this IServiceCollection self, Type type, object? key, FactoryDelegate factory, LifetimeBase lifetime, ServiceOptions? options = null)
            => factory is not null
                ? self.Factory(type, key, (scope, type) => factory(scope, type), lifetime, options)
                : throw new ArgumentNullException(nameof(factory));

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Factory(typeof(IMyService), (scope, type) => new MyService(...), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once.</param>
        /// <param name="factoryExpr">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter. Note that the second parameter of the <paramref name="factoryExpr"/> is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You can register generic services (where the <paramref name="type"/> parameter is an open generic type).</remarks>
        [OverloadResolutionPriority(1)]
        public static IServiceCollection Factory(this IServiceCollection self, Type type, Expression<FactoryDelegate> factoryExpr, LifetimeBase lifetime, ServiceOptions? options = null) 
            => self.Factory(type, null, factoryExpr, lifetime, options);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Factory(typeof(IMyService), (scope, type) => new MyService(...), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter. Note that the second parameter of the <paramref name="factory"/> is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can register generic services (where the <paramref name="type"/> parameter is an open generic type).</item>
        /// <item>It's advised to utilize <paramref name="factory"/> expressions (using the <see cref="Factory(IServiceCollection, Type, Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> method) insted of real factories as they are more performant.</item>
        /// </list>
        /// </remarks>
        public static IServiceCollection Factory(this IServiceCollection self, Type type, FactoryDelegate factory, LifetimeBase lifetime, ServiceOptions? options = null)
            => self.Factory(type, null, factory, lifetime, options);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Factory&lt;IMyService&gt;("serviceName", scope => new MyService(...), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TType">The service type to be registered. It can be registered only once (with the given <paramref name="key"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="factoryExpr">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        [OverloadResolutionPriority(1)]
        public static IServiceCollection Factory<TType>(this IServiceCollection self, object? key, Expression<FactoryDelegate<TType>> factoryExpr, LifetimeBase lifetime, ServiceOptions? options = null) where TType : class
            => self.Factory(typeof(TType), key, WrapToStandardDelegate(factoryExpr), lifetime, options);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Factory&lt;IMyService&gt;("serviceName", scope => new MyService(...), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TType">The service type to be registered. It can be registered only once (with the given <paramref name="key"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can register generic services (where the <typeparamref name="TType"/> is an open generic type).</item>
        /// <item>It's advised to utilize <paramref name="factory"/> expressions (using the <see cref="Factory{TType}(IServiceCollection, object?, Expression{FactoryDelegate{TType}}, LifetimeBase, ServiceOptions?)"/> method) insted of real factories as they are more performant.</item>
        /// </list>
        /// </remarks>
        public static IServiceCollection Factory<TType>(this IServiceCollection self, object? key, FactoryDelegate<TType> factory, LifetimeBase lifetime, ServiceOptions? options = null) where TType : class
            => factory is not null
                ? self.Factory(key, scope => factory(scope), lifetime, options)
                : throw new ArgumentNullException(nameof(factory));

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Factory&lt;IMyService&gt;(scope => new MyService(...), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TType">The service type to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="factoryExpr">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        [OverloadResolutionPriority(1)]
        public static IServiceCollection Factory<TType>(this IServiceCollection self, Expression<FactoryDelegate<TType>> factoryExpr, LifetimeBase lifetime, ServiceOptions? options = null) where TType : class
            => self.Factory(typeof(TType), null, WrapToStandardDelegate(factoryExpr), lifetime, options);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Factory&lt;IMyService&gt;(scope => new MyService(...), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TType">The service type to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can register generic services (where the <typeparamref name="TType"/> is an open generic type).</item>
        /// <item>It's advised to utilize <paramref name="factory"/> expressions (using the <see cref="Factory{TType}(IServiceCollection, Expression{FactoryDelegate{TType}}, LifetimeBase, ServiceOptions?)"/> method) insted of real factories as they are more performant.</item>
        /// </list>
        /// </remarks>
        public static IServiceCollection Factory<TType>(this IServiceCollection self, FactoryDelegate<TType> factory, LifetimeBase lifetime, ServiceOptions? options = null) where TType : class
            => self.Factory(null, factory, lifetime, options);

        private static Expression<FactoryDelegate> WrapToStandardDelegate<TInterface>(Expression<FactoryDelegate<TInterface>> factoryExpr) where TInterface : class
        {
            if (factoryExpr is null)
                throw new ArgumentNullException(nameof(factoryExpr));

            ParameterExpression
                injector = factoryExpr.Parameters[0],
                type = Expression.Parameter(typeof(Type), nameof(type));

            return Expression.Lambda<FactoryDelegate>
            (
                factoryExpr.Body,
                injector,
                type
            );
        }
    }
}