/********************************************************************************
* IServiceContainerExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI
{
    using Properties;
    using Internals;
    using Annotations;

    using Utils.Proxy;

    /// <summary>
    /// Defines several handy extensions for the <see cref="IServiceContainer"/> interface.
    /// </summary>
    public static partial class IServiceContainerExtensions
    {
        /// <summary>
        /// Gets the service entry associated with the given interface and name.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the entry.</param>
        /// <param name="mode">Options.</param>
        /// <returns>The requested service entry.</returns>
        /// <exception cref="ServiceNotFoundException">If the service could not be found.</exception>
        public static AbstractServiceEntry Get<TInterface>(this IServiceContainer self, string name, QueryModes mode = QueryModes.Default)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return self.Get(typeof(TInterface), name, mode);
        }

        /// <summary>
        /// Gets the service entry associated with the given interface.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="mode">Options.</param>
        /// <returns>The requested service entry.</returns>
        /// <exception cref="ServiceNotFoundException">If the service could not be found.</exception>
        public static AbstractServiceEntry Get<TInterface>(this IServiceContainer self, QueryModes mode = QueryModes.Default) => self.Get<TInterface>(null, mode);

        /// <summary>
        /// Registers a new service with the given implementation.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerExtensions.Factory(IServiceContainer, Type, Func{IInjector, Type, object}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceContainer Service(this IServiceContainer self, Type iface, string name, Type implementation, Lifetime lifetime = Lifetime.Transient)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return self.Add
            (
                ProducibleServiceEntry.Create(lifetime, iface, name, implementation, self)
            );           
        }

        /// <summary>
        /// Registers a new service with the given implementation.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerExtensions.Factory(IServiceContainer, Type, Func{IInjector, Type, object}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceContainer Service(this IServiceContainer self, Type iface, Type implementation, Lifetime lifetime = Lifetime.Transient) => self.Service(iface, null, implementation, lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The <see cref="ITypeResolver"/> is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter. For an implementation see the <see cref="LazyTypeResolver{TInterface}"/> class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where the <paramref name="iface"/> parameter is an open generic <see cref="Type"/>). In this case the resolver must return an open generic implementation.</remarks>
        public static IServiceContainer Lazy(this IServiceContainer self, Type iface, string name, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return self.Add
            (
                ProducibleServiceEntry.Create(lifetime, iface, name, implementation, self)
            );
        }

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The <see cref="ITypeResolver"/> is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter. For an implementation see the <see cref="LazyTypeResolver{TInterface}"/> class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where the <paramref name="iface"/> parameter is an open generic <see cref="Type"/>). In this case the resolver must return an open generic implementation.</remarks>
        public static IServiceContainer Lazy(this IServiceContainer self, Type iface, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient) => self.Lazy(iface, null, implementation, lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request. Type resolution is done by the <see cref="LazyTypeResolver"/>.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="asmPath">The absolute path of the containing <see cref="System.Reflection.Assembly"/>.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <paramref name="iface"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy(this IServiceContainer self, Type iface, string name, string asmPath, string className, Lifetime lifetime = Lifetime.Transient) => 
            self.Lazy(iface, name, new LazyTypeResolver(iface, asmPath, className), lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request. Type resolution is done by the <see cref="LazyTypeResolver"/>.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="asmPath">The absolute path of the containing <see cref="System.Reflection.Assembly"/>.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <paramref name="iface"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy(this IServiceContainer self, Type iface, string asmPath, string className, Lifetime lifetime = Lifetime.Transient) =>
            self.Lazy(iface, null, asmPath, className, lifetime);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name  of the service.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter. Note that the second parameter of the factory is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can register generic services (where the <paramref name="iface"/> parameter is an open generic type).</remarks>
        public static IServiceContainer Factory(this IServiceContainer self, Type iface, string name, Func<IInjector, Type, object> factory, Lifetime lifetime = Lifetime.Transient)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return self.Add(ProducibleServiceEntry.Create(lifetime, iface, name, factory, self));
        }

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter. Note that the second parameter of the factory is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can register generic services (where the <paramref name="iface"/> parameter is an open generic type).</remarks>
        public static IServiceContainer Factory(this IServiceContainer self, Type iface, Func<IInjector, Type, object> factory, Lifetime lifetime = Lifetime.Transient) => self.Factory(iface, null, factory, lifetime);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service to be decorated.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of the decorator function.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against generic, instance or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceContainer Proxy(this IServiceContainer self, Type iface, string name, Func<IInjector, Type, object, object> decorator)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (decorator == null)
                throw new ArgumentNullException(nameof(decorator));

            AbstractServiceEntry entry = self.Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError);

            //
            // Generikus szerviz, Abstract(), Instance() eseten valamint ha nem ez a 
            // tarolo birtokolja az adott bejegyzest a metodus nem ertelmezett.
            //

            if (entry.Owner != self || entry.Factory == null)
                throw new InvalidOperationException(Resources.CANT_PROXY);

            //
            // Bovitjuk a hivasi lancot a decorator-al.
            //

            Func<IInjector, Type, object> oldFactory = entry.Factory;
            entry.Factory = (injector, type) => decorator(injector, type, oldFactory(injector, type));

            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service to be decorated.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of the decorator function.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against generic, instance or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceContainer Proxy(this IServiceContainer self, Type iface, Func<IInjector, Type, object, object> decorator) => self.Proxy(iface, null, decorator);

        /// <summary>
        /// Registers a pre-created instance. Useful to creating "constant" values (e.g. command-line arguments).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <paramref name="iface"/> interface.</param>
        /// <param name="releaseOnDispose">Whether the system should dispose the instance on container disposal or not.</param>
        /// <returns>The container itself.</returns>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The container is responsible for disposing the entry.")]
        public static IServiceContainer Instance(this IServiceContainer self, Type iface, string name, object instance, bool releaseOnDispose = false)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return self.Add(new InstanceServiceEntry(iface, name, instance, releaseOnDispose, self));
        }

        /// <summary>
        /// Registers a pre-created instance. Useful to creating "constant" values (e.g. command-line arguments).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <paramref name="iface"/> interface.</param>
        /// <param name="releaseOnDispose">Whether the system should dispose the instance on container disposal or not.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Instance(this IServiceContainer self, Type iface, object instance, bool releaseOnDispose = false) => self.Instance(iface, null, instance, releaseOnDispose);

        /// <summary>
        /// Registers an abstract service. It must be overridden in the child container(s).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The container itself.</returns>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The container is responsible for disposing the entry.")]
        public static IServiceContainer Abstract(this IServiceContainer self, Type iface, string name = null)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            return self.Add(new AbstractServiceEntry(iface, name));
        }

        /// <summary>
        /// Creates a new <see cref="IInjector"/> instance from this container.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <returns>The newly created <see cref="IInjector"/> instance.</returns>
        /// <remarks><see cref="IInjector"/> represents also a scope.</remarks>
        /// <exception cref="InvalidOperationException">There are one or more abstract entries in the collection.</exception>
        public static IInjector CreateInjector(this IServiceContainer self) => new Injector(self ?? throw new ArgumentNullException(nameof(self)));

        /// <summary>
        /// Registers a new service.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerExtensions.Factory{TInterface}(IServiceContainer, Func{IInjector, TInterface}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Service<TInterface, TImplementation>(this IServiceContainer self, Lifetime lifetime = Lifetime.Transient) where TImplementation: TInterface => self.Service(typeof(TInterface), typeof(TImplementation), lifetime);

        /// <summary>
        /// Registers a new service.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerExtensions.Factory{TInterface}(IServiceContainer, Func{IInjector, TInterface}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Service<TInterface, TImplementation>(this IServiceContainer self, string name, Lifetime lifetime = Lifetime.Transient) where TImplementation : TInterface => self.Service(typeof(TInterface), name, typeof(TImplementation), lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="implementation">The <see cref="ITypeResolver"/> is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <typeparamref name="TInterface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter. For an implementation see the <see cref="LazyTypeResolver{TInterface}"/> class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy<TInterface>(this IServiceContainer self, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient) => self.Lazy<TInterface>(null, implementation, lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The <see cref="ITypeResolver"/> is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <typeparamref name="TInterface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter. For an implementation see the <see cref="LazyTypeResolver{TInterface}"/> class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy<TInterface>(this IServiceContainer self, string name, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient) => self.Lazy(typeof(TInterface), name, implementation, lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request. Type resolution is done by the <see cref="LazyTypeResolver"/>.
        /// </summary>
        ///<typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="asmPath">The absolute path of the containing <see cref="System.Reflection.Assembly"/>.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <typeparamref name="TInterface"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy<TInterface>(this IServiceContainer self, string name, string asmPath, string className, Lifetime lifetime = Lifetime.Transient) =>
            self.Lazy(typeof(TInterface), name, asmPath, className, lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request. Type resolution is done by the <see cref="LazyTypeResolver"/>.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="asmPath">The absolute path of the containing <see cref="System.Reflection.Assembly"/>.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <typeparamref name="TInterface"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy<TInterface>(this IServiceContainer self, string asmPath, string className, Lifetime lifetime = Lifetime.Transient) =>
            self.Lazy<TInterface>(null, asmPath, className, lifetime);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Factory<TInterface>(this IServiceContainer self, string name, Func<IInjector, TInterface> factory, Lifetime lifetime = Lifetime.Transient) => self.Factory(typeof(TInterface), name, (injector, type) => factory(injector), lifetime);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Factory<TInterface>(this IServiceContainer self, Func<IInjector, TInterface> factory, Lifetime lifetime = Lifetime.Transient) => self.Factory(null, injector => factory(injector), lifetime);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <typeparam name="TInterface">The service to be decorated.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 2nd parameter of the decorator function.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see above).</exception>
        public static IServiceContainer Proxy<TInterface>(this IServiceContainer self, string name, Func<IInjector, TInterface, TInterface> decorator) => self.Proxy(typeof(TInterface), name, (injector, type, instance) => decorator(injector, (TInterface) instance));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <typeparam name="TInterface">The service to be decorated.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 2nd parameter of the decorator function.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see above).</exception>
        public static IServiceContainer Proxy<TInterface>(this IServiceContainer self, Func<IInjector, TInterface, TInterface> decorator) => self.Proxy(typeof(TInterface), null, (injector, type, instance) => decorator(injector, (TInterface)instance));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <typeparam name="TInterface">The service to be decorated.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see remarks).</exception>
        public static IServiceContainer Proxy<TInterface, TInterceptor>(this IServiceContainer self, string name = null) where TInterface: class where TInterceptor: InterfaceInterceptor<TInterface> => self.Proxy<TInterface>(name, (injector, instance) => ProxyFactory.Create<TInterface, TInterceptor>(instance, injector));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The interface to be intercepted.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="interceptor">The interceptor class.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see remarks).</exception>
        public static IServiceContainer Proxy(this IServiceContainer self, Type iface, string name, Type interceptor) => self.Proxy(iface, name, (injector, type, instance) => ProxyFactory.Create(iface, interceptor, instance, injector));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The interface to be intercepted.</param>
        /// <param name="interceptor">The interceptor class.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see remarks).</exception>
        public static IServiceContainer Proxy(this IServiceContainer self, Type iface, Type interceptor) => self.Proxy(iface, null, interceptor);

        /// <summary>
        /// Registers a pre-created instance. Useful when creating "constant" values (e.g. from command-line arguments).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="instance">The pre-created instance to be registered.</param>
        /// <param name="releaseOnDispose">Whether the system should dispose the instance on container disposal or not.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Instance<TInterface>(this IServiceContainer self, string name, TInterface instance, bool releaseOnDispose = false) => self.Instance(typeof(TInterface), name, instance, releaseOnDispose);

        /// <summary>
        /// Registers a pre-created instance. Useful when creating "constant" values (e.g. from command-line arguments).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="instance">The pre-created instance to be registered.</param>
        /// <param name="releaseOnDispose">Whether the system should dispose the instance on container disposal or not.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Instance<TInterface>(this IServiceContainer self, TInterface instance, bool releaseOnDispose = false) => self.Instance(null, instance, releaseOnDispose);

        /// <summary>
        /// Registers an abstract service. It must be overridden in the child container(s).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Abstract<TInterface>(this IServiceContainer self, string name = null) => self.Abstract(typeof(TInterface), name);
    }
}