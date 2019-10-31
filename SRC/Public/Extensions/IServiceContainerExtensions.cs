/********************************************************************************
* IServiceContainerExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
#if NETSTANDARD1_6
using System.Reflection;
#endif

namespace Solti.Utils.DI
{
    using Properties;
    using Internals;
    using Annotations;
    using Proxy;

    /// <summary>
    /// Defines several handy extensions for the <see cref="IServiceContainer"/> interface.
    /// </summary>
    public static class IServiceContainerExtensions
    {
        private static object TypeChecker(IInjector injector, Type type, object inst)
        {
            //
            // A letrhozott peldany tipusat ellenorizzuk. 
            //

            if (!type.IsInstanceOfType(inst))
                throw new Exception(string.Format(Resources.INVALID_INSTANCE, type));

            return inst;
        }

        /// <summary>
        /// Gets the service associated with the given interface.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the entry.</param>
        /// <param name="mode">Options.</param>
        /// <returns>The requested service entry.</returns>
        /// <exception cref="ServiceNotFoundException">If the service could not be found.</exception>
        public static AbstractServiceEntry Get<TInterface>(this IServiceContainer self, string name, QueryMode mode = QueryMode.Default) => self.Get(typeof(TInterface), name, mode);

        /// <summary>
        /// Gets the service associated with the given interface.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="mode">Options.</param>
        /// <returns>The requested service entry.</returns>
        /// <exception cref="ServiceNotFoundException">If the service could not be found.</exception>
        public static AbstractServiceEntry Get<TInterface>(this IServiceContainer self, QueryMode mode = QueryMode.Default) => self.Get<TInterface>(null, mode);

        /// <summary>
        /// Registers a new service with the given implementation.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerExtensions.Factory(IServiceContainer, Type, Func{IInjector, Type, object}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceContainer Service(this IServiceContainer self, Type iface, string name, Type implementation, Lifetime lifetime = Lifetime.Transient)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (iface == null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            if (implementation == null)
                throw new ArgumentNullException(nameof(implementation));

            //
            // Implementacio validalas (mukodik generikusokra is).
            //

            if (!iface.IsInterfaceOf(implementation))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, implementation));

            //
            // Konstruktor validalas csak generikus esetben kell (mert ilyenkor nincs Resolver.Get()
            // hivas). A GetApplicableConstructor() validal valamint mukodik generikusokra is.
            // 

            if (implementation.IsGenericTypeDefinition())
                implementation.GetApplicableConstructor();

            return self.Add
            (
                ProducibleServiceEntryFactory.CreateEntry(lifetime, iface, name, implementation, self)
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
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The <see cref="ITypeResolver"/> is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter. For an implementation see the <see cref="LazyTypeResolver{TInterface}"/> class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where the <paramref name="iface"/> parameter is an open generic <see cref="Type"/>). In this case the resolver must return an open generic implementation.</remarks>
        public static IServiceContainer Lazy(this IServiceContainer self, Type iface, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (iface == null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            if (implementation == null)
                throw new ArgumentNullException(nameof(implementation));

            //
            // A konstruktort validalni fogja a Resolver.Get() hivas az elso peldanyositaskor, igy
            // itt nekunk csak azt kell ellenoriznunk, h az interface tamogatott e.
            //

            if (!implementation.Supports(iface))
                throw new NotSupportedException(string.Format(Resources.INTERFACE_NOT_SUPPORTED, iface));

            return self.Add
            (
                ProducibleServiceEntryFactory.CreateEntry(lifetime, iface, null, implementation, self)
            );
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
        public static IServiceContainer Factory(this IServiceContainer self, Type iface, Func<IInjector, Type, object> factory, Lifetime lifetime = Lifetime.Transient)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (iface == null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            return self
                .Add(ProducibleServiceEntryFactory.CreateEntry(lifetime, iface, null, factory, self))
                
                //
                // A kivulrol legyartott peldany tipusat meg ellenorizzuk.
                //

                .Proxy(iface, TypeChecker);
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
        public static IServiceContainer Proxy(this IServiceContainer self, Type iface, Func<IInjector, Type, object, object> decorator)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (iface == null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            if (decorator == null)
                throw new ArgumentNullException(nameof(decorator));

            AbstractServiceEntry entry = self.Get(iface, null, QueryMode.AllowSpecialization | QueryMode.ThrowOnError);

            //
            // Generikus szerviz, Abstract(), Instance() eseten valamint ha nem ez a 
            // tarolo birtokolja az adott bejegyzest a metodus nem ertelmezett.
            //

            if (entry.Owner != self || entry.Factory == null)
                throw new InvalidOperationException(Resources.CANT_PROXY);

            Install(decorator);

            //
            // A kivulrol legyartott peldany tipusat meg ellenorizzuk.
            //

            Install(TypeChecker);

            return self;

            void Install(Func<IInjector, Type, object, object> fn)
            {
                Func<IInjector, Type, object> oldFactory = entry.Factory;

                entry.Factory = (injector, type) => fn(injector, type, oldFactory(injector, type));
            }
        }

        /// <summary>
        /// Registers a pre-created instance. Useful to creating "constant" values (e.g. command-line arguments).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only onc.</param>
        /// <param name="instance">The pre-created instance to be registered. It can not be null and must implement the <paramref name="iface"/> interface.</param>
        /// <param name="releaseOnDispose">Whether the system should dispose the instance on container disposal or not.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Instance(this IServiceContainer self, Type iface, object instance, bool releaseOnDispose = false)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (iface == null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!iface.IsInstanceOfType(instance))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, instance.GetType()));

            return self.Add(new InstanceServiceEntry(iface, null, instance, releaseOnDispose, self));
        }

        /// <summary>
        /// Registers an abstract service. It must be overridden in the child container(s).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Abstract(this IServiceContainer self, Type iface)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (iface == null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            return self.Add(new AbstractServiceEntry(iface, null));
        }

        /// <summary>
        /// Creates a new <see cref="IInjector"/> instance from this container.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <returns>The newly created <see cref="IInjector"/> instance.</returns>
        /// <remarks>The lifetime of the returned <see cref="IInjector"/> is controlled by its parent. Despite this you may dispose it manually.</remarks>
        /// <exception cref="InvalidOperationException">There are one or more abstract entries in the collection.</exception>
        public static IInjector CreateInjector(this IServiceContainer self)
        {
            Type[] abstractEntries = self
                .Where(entry => entry.GetType() == typeof(AbstractServiceEntry))
                .Select(entry => entry.Interface)
                .ToArray();

            if (abstractEntries.Any())
            {
                var ioEx = new InvalidOperationException(Resources.INVALID_INJECTOR_ENTRY);
                ioEx.Data.Add(nameof(abstractEntries), abstractEntries);
                throw ioEx;
            }

            return new Injector(self);
        }

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
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
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
        public static IServiceContainer Lazy<TInterface>(this IServiceContainer self, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient) => self.Lazy(typeof(TInterface), implementation, lifetime);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Factory<TInterface>(this IServiceContainer self, Func<IInjector, TInterface> factory, Lifetime lifetime = Lifetime.Transient) => self.Factory(typeof(TInterface), (injector, type) => factory(injector), lifetime);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <typeparam name="TInterface">The service to be decorated.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 2nd parameter of the decorator function.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see above).</exception>
        public static IServiceContainer Proxy<TInterface>(this IServiceContainer self, Func<IInjector, TInterface, TInterface> decorator) => self.Proxy(typeof(TInterface), (injector, type, instance) => decorator(injector, (TInterface) instance));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <typeparam name="TInterface">The service to be decorated.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see remarks).</exception>
        public static IServiceContainer Proxy<TInterface, TInterceptor>(this IServiceContainer self) where TInterface: class where TInterceptor: InterfaceInterceptor<TInterface> => self.Proxy<TInterface>((injector, instance) => ProxyFactory.Create<TInterface, TInterceptor>(instance, injector));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The interface to be intercepted.</param>
        /// <param name="interceptor">The interceptor class.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see remarks).</exception>
        public static IServiceContainer Proxy(this IServiceContainer self, Type iface, Type interceptor) => self.Proxy(iface, (injector, type, instance) => ProxyFactory.Create(iface, interceptor, instance, injector));

        /// <summary>
        /// Registers a pre-created instance. Useful when creating "constant" values (e.g. from command-line arguments).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="instance">The pre-created instance to be registered.</param>
        /// <param name="releaseOnDispose">Whether the system should dispose the instance on container disposal or not.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Instance<TInterface>(this IServiceContainer self, TInterface instance, bool releaseOnDispose = false) => self.Instance(typeof(TInterface), instance, releaseOnDispose);

        /// <summary>
        /// Registers an abstract service. It must be overridden in the child container(s).
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Abstract<TInterface>(this IServiceContainer self) => self.Abstract(typeof(TInterface));
    }
}