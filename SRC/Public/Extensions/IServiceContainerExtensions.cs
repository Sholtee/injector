/********************************************************************************
* IServiceContainerExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Annotations;
    using Proxy;

    /// <summary>
    /// Defines several handy extensions for the <see cref="IServiceContainer"/> interface.
    /// </summary>
    public static class IServiceContainerExtensions
    {
        /// <summary>
        /// Registers a new service with the given type.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparam name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerExtensions.Factory{TInterface}(IServiceContainer, Func{IInjector, TInterface}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The container itself.</param>
        /// <param name="lifetime">The lifetime of the service. For more information see the <see cref="Lifetime"/> enum.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Service<TInterface, TImplementation>(this IServiceContainer self, Lifetime lifetime = Lifetime.Transient) where TImplementation: TInterface => self.Service(typeof(TInterface), typeof(TImplementation), lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The container itself.</param>
        /// <param name="implementation">The resolver (<see cref="ITypeResolver"/>) is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <typeparam name="TInterface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The lifetime of the service. For more information see the <see cref="Lifetime"/> enum.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy<TInterface>(this IServiceContainer self, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient) => self.Lazy(typeof(TInterface), implementation, lifetime);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The container itself.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The lifetime of the service. For more information see the <see cref="Lifetime"/> enum.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Factory<TInterface>(this IServiceContainer self, Func<IInjector, TInterface> factory, Lifetime lifetime = Lifetime.Transient) => self.Factory(typeof(TInterface), (me, type) => factory(me), lifetime);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easyest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service to be decorated.</typeparam>
        /// <param name="self">The container itself.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 2nd parameter.</param>
        /// <returns>The container itself.</returns>
        ///<remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        public static IServiceContainer Proxy<TInterface>(this IServiceContainer self, Func<IInjector, TInterface, TInterface> decorator) => self.Proxy(typeof(TInterface), (me, type, instance) => decorator(me, (TInterface) instance));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easyest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service to be decorated.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class.</typeparam>
        /// <param name="self">The container itself.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        public static IServiceContainer Proxy<TInterface, TInterceptor>(this IServiceContainer self) where TInterface: class where TInterceptor: InterfaceInterceptor<TInterface> => self.Proxy<TInterface>((me, instance) => ProxyFactory.Create<TInterface, TInterceptor>(instance, me));

        /// <summary>
        /// Registers a pre-created instance. Useful when creating "constant" values (e.g. from command-line arguments).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The container itself.</param>
        /// <param name="instance">The pre-created instance to be registered.</param>
        /// <param name="releaseOnDispose">Wheter the system should dispose the instance on injector disposal.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Instance<TInterface>(this IServiceContainer self, TInterface instance, bool releaseOnDispose = false) => self.Instance(typeof(TInterface), instance, releaseOnDispose);
    }
}