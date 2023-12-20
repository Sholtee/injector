/********************************************************************************
* Service.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    using static Properties.Resources;

    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Registers a new named service with the given implementation:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Service(typeof(IMyService), "serviceName", typeof(MyService), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once (with the given <paramref name="key"/>).</param>
        /// <param name="key">The (optional) key of the service (usually a name).</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the service <paramref name="type"/>. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type, object?, Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceCollection Service(this IServiceCollection self, Type type, object? key, Type implementation, LifetimeBase lifetime, ServiceOptions? options = null)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (implementation is null)
                throw new ArgumentNullException(nameof(implementation));

            if (!type.IsAssignableFrom(implementation))
                throw new ArgumentException(string.Format(Culture, NOT_IMPLEMENTED, type), nameof(type));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            return self.Register
            (
                lifetime.CreateFrom(type, key, implementation, options ?? ServiceOptions.Default)
            );
        }

        /// <summary>
        /// Registers a new named service using arbitrary constructor arguments:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Service(typeof(IMyService), "serviceName", typeof(MyService), new {ctorParam = ...}, Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once (with the given <paramref name="key"/>).</param>
        /// <param name="key">The (optional) key of the service (usually a name).</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the service <paramref name="type"/>. Additionally it should have only one public constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type, Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user (may be an anonym object or a <see cref="IReadOnlyDictionary{TKey, TValue}"/> where the key is <see cref="string"/> and value is <see cref="object"/>).</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceCollection Service(this IServiceCollection self, Type type, object? key, Type implementation, object explicitArgs, LifetimeBase lifetime, ServiceOptions? options = null)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (implementation is null)
                throw new ArgumentNullException(nameof(implementation));

            if (!type.IsAssignableFrom(implementation))
                throw new ArgumentException(string.Format(Culture, NOT_IMPLEMENTED, type), nameof(type));

            if (explicitArgs is null)
                throw new ArgumentNullException(nameof(explicitArgs));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            return self.Register
            (
                lifetime.CreateFrom(type, key, implementation, explicitArgs, options ?? ServiceOptions.Default)
            );
        }

        /// <summary>
        /// Registers a new service with the given implementation:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Service(typeof(IMyService), typeof(MyService), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the service <paramref name="type"/>. Additionally it should have only one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type, Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceCollection Service(this IServiceCollection self, Type type, Type implementation, LifetimeBase lifetime, ServiceOptions? options = null) 
            => self.Service(type, key: null, implementation, lifetime, options);

        /// <summary>
        /// Registers a new service using arbitrary constructor arguments:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Service(typeof(IMyService), typeof(MyService), new {ctorParam = ...}, Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the service <paramref name="type"/>. Additionally it should have only one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type,Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user (may be an anonym object or a <see cref="IReadOnlyDictionary{TKey, TValue}"/> where the key is <see cref="string"/> and value is <see cref="object"/>).</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceCollection Service(this IServiceCollection self, Type type, Type implementation, object explicitArgs, LifetimeBase lifetime, ServiceOptions? options = null)
            => self.Service(type, null, implementation, explicitArgs, lifetime, options);

        /// <summary>
        /// Registers a new service with the given implementation:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Service&lt;IMyService, MyService&gt;(Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TType">The service type to be registered. It can be registered only once.</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the service <typeparamref name="TType"/> and should have only one public constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory{TType}(IServiceCollection, Expression{FactoryDelegate{TType}}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Service<TType, TImplementation>(this IServiceCollection self, LifetimeBase lifetime, ServiceOptions? options = null) where TType : class where TImplementation: TType 
            => self.Service(typeof(TType), typeof(TImplementation), lifetime, options);

        /// <summary>
        /// Registers a new named service with the given implementation:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Service&lt;IMyService, MyService&gt;("serviceName", Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TType">The service type to be registered. It can be registered only once (with the given <paramref name="key"/>).</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the service <typeparamref name="TType"/> and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory{TType}(IServiceCollection, Expression{FactoryDelegate{TType}}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) key of the service (usually a name).</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Service<TType, TImplementation>(this IServiceCollection self, object? key, LifetimeBase lifetime, ServiceOptions? options = null) where TType : class where TImplementation : TType 
            => self.Service(typeof(TType), key, typeof(TImplementation), lifetime, options);

        /// <summary>
        /// Registers a new named service using arbitrary constructor arguments:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Service&lt;IMyService, MyService&gt;("serviceName", new {ctorParam = ...}, Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TType">The service type to be registered. It can be registered only once (with the given <paramref name="key"/>).</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the service <typeparamref name="TType"/> and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory{TType}(IServiceCollection, Expression{FactoryDelegate{TType}}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) key of the service (usually a name).</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user (may be an anonym object or a <see cref="IReadOnlyDictionary{TKey, TValue}"/> where the key is <see cref="string"/> and value is <see cref="object"/>).</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Service<TType, TImplementation>(this IServiceCollection self, object? key, object explicitArgs, LifetimeBase lifetime, ServiceOptions? options = null) where TType : class where TImplementation : TType
            => self.Service(typeof(TType), key, typeof(TImplementation), explicitArgs, lifetime, options);
    }
}