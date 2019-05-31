/********************************************************************************
* IInjector.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Internals;

    /// <summary>
    /// Provides a mechanism for injecting resources.
    /// </summary>
    public interface IInjector: IComposite<IInjector>, IStateful
    {
        /// <summary>
        /// Registers a new service with the given type.
        /// </summary>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies).</param>
        /// <param name="lifetime">The lifetime of the service. For more information see the <see cref="Lifetime"/> enum.</param>
        /// <returns>The injector itself.</returns>
        /// <remarks>You can register generic services (where the interface and the implementation are open generic types). The system will specialize the implementation if you request a concrete service.</remarks>
        [StateCritical] 
        IInjector Service([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull), typeof(Class))] Type implementation, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request.
        /// </summary>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="resolver">The resolver (<see cref="IResolver"/>) is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <see cref="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The lifetime of the service. For more information see the <see cref="Lifetime"/> enum.</param>
        /// <returns>The injector itself.</returns>
        /// <remarks>You can register generic services (where the <paramref name="iface"/> parameter is an open generic type). In this case the resolver must return an open generic implementation.</remarks>
        [StateCritical]
        IInjector Lazy([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull))] IResolver resolver, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter. Note that the second parameter is never generic, not even if you registered the factory for an open generic interface.</param>
        /// <param name="lifetime">The lifetime of the service. For more information see the <see cref="Lifetime"/> enum.</param>
        /// <returns>The injector itself.</returns>
        /// <remarks>You can register generic services (where the <paramref name="iface"/> parameter is an open generic type).</remarks>
        [StateCritical]
        IInjector Factory([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull))] Func<IInjector, Type, object> factory, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easyest way to decorate an instance is using the <see cref="InterfaceProxy{TInterface}"/> class.
        /// </summary>
        /// <param name="iface">The "id" of the service to be decorated.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter.</param>
        /// <returns>The injector itself.</returns>
        /// <remarks>You can't create proxies against generic or instance entries. A service can be decorated multiple times.</remarks>
        [StateCritical]
        IInjector Proxy([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull))] Func<IInjector, Type, object, object> decorator);

        /// <summary>
        /// Registers a pre-created instance. Useful to creating "constant" values (e.g. command-line arguments).
        /// </summary>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <see cref="iface"/> interface.</param>
        /// <param name="releaseOnDispose">Wheter the system should dispose the instance on injector disposal.</param>
        /// <returns>The injector itself.</returns>
        [StateCritical]
        IInjector Instance([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull))] object instance, bool releaseOnDispose = true);

        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be a non-generic interface.</param>
        /// <returns>The resolved service.</returns>
        /// <remarks>This method is thread safe so you can call it parallelly.</remarks>
        /// <exception cref="NotSupportedException">The service can not be found.</exception>
        object Get([ParameterIs(typeof(NotNull), typeof(Interface), typeof(NotGeneric))] Type iface);

        /// <summary>
        /// Registered entries. It contains the explicit and implicit entries as well.
        /// </summary>
        IReadOnlyList<Type> Entries { get; }

        /// <summary>
        /// Gets basic informations about a registered service.
        /// </summary>
        /// <param name="iface">The "id" of the service to be queried. It must be an interface.</param>
        /// <returns>An <see cref="IServiceInfo"/> instance.</returns>
        /// <exception cref="NotSupportedException">The service can not be found.</exception>
        IServiceInfo QueryServiceInfo([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface);
    }
}