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
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type, string?, Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceCollection Service(this IServiceCollection self, Type iface, string? name, Type implementation, LifetimeBase lifetime, ServiceOptions? options = null)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (implementation is null)
                throw new ArgumentNullException(nameof(implementation));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            return self.Register
            (
                lifetime.CreateFrom(iface, name, implementation, options ?? ServiceOptions.Default)
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
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it should have only one public constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type, Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user (may be an anonym object or a <see cref="IReadOnlyDictionary{TKey, TValue}"/> where the key is <see cref="string"/> and value is <see cref="object"/>).</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceCollection Service(this IServiceCollection self, Type iface, string? name, Type implementation, object explicitArgs, LifetimeBase lifetime, ServiceOptions? options = null)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (implementation is null)
                throw new ArgumentNullException(nameof(implementation));

            if (explicitArgs is null)
                throw new ArgumentNullException(nameof(explicitArgs));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            return self.Register
            (
                lifetime.CreateFrom(iface, name, implementation, explicitArgs, options ?? ServiceOptions.Default)
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
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it should have only one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type, Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceCollection Service(this IServiceCollection self, Type iface, Type implementation, LifetimeBase lifetime, ServiceOptions? options = null) 
            => self.Service(iface, null, implementation, lifetime, options);

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
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it should have only one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type,Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user (may be an anonym object or a <see cref="IReadOnlyDictionary{TKey, TValue}"/> where the key is <see cref="string"/> and value is <see cref="object"/>).</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceCollection Service(this IServiceCollection self, Type iface, Type implementation, object explicitArgs, LifetimeBase lifetime, ServiceOptions? options = null)
            => self.Service(iface, null, implementation, explicitArgs, lifetime, options);

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
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and should have only one public constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory{TInterface}(IServiceCollection, Expression{FactoryDelegate{TInterface}}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Service<TInterface, TImplementation>(this IServiceCollection self, LifetimeBase lifetime, ServiceOptions? options = null) where TInterface : class where TImplementation: TInterface 
            => self.Service(typeof(TInterface), typeof(TImplementation), lifetime, options);

        /// <summary>
        /// Registers a new service using arbitrary constructor arguments:
        /// <code>
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Service&lt;IMyService, MyService&gt;(new {ctorParam = ...}, Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and should have only one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory{TInterface}(IServiceCollection, Expression{FactoryDelegate{TInterface}}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user (may be an anonym object or a <see cref="IReadOnlyDictionary{TKey, TValue}"/> where the key is <see cref="string"/> and value is <see cref="object"/>).</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Service<TInterface, TImplementation>(this IServiceCollection self, object explicitArgs, LifetimeBase lifetime, ServiceOptions? options = null) where TInterface : class where TImplementation : TInterface
            => self.Service(typeof(TInterface), typeof(TImplementation), explicitArgs, lifetime, options);

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
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory{TInterface}(IServiceCollection, Expression{FactoryDelegate{TInterface}}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Service<TInterface, TImplementation>(this IServiceCollection self, string name, LifetimeBase lifetime, ServiceOptions? options = null) where TInterface : class where TImplementation : TInterface 
            => self.Service(typeof(TInterface), name, typeof(TImplementation), lifetime, options);

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
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceCollectionBasicExtensions.Factory{TInterface}(IServiceCollection, Expression{FactoryDelegate{TInterface}}, LifetimeBase, ServiceOptions?)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user (may be an anonym object or a <see cref="IReadOnlyDictionary{TKey, TValue}"/> where the key is <see cref="string"/> and value is <see cref="object"/>).</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Service<TInterface, TImplementation>(this IServiceCollection self, string name, object explicitArgs, LifetimeBase lifetime, ServiceOptions? options = null) where TInterface : class where TImplementation : TInterface
            => self.Service(typeof(TInterface), name, typeof(TImplementation), explicitArgs, lifetime, options);
    }
}