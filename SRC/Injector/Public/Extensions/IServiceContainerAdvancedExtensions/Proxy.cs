/********************************************************************************
* Proxy.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;
    using Properties;   

    using Utils.Proxy;

    public static partial class IServiceContainerAdvancedExtensions
    {
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
        public static IServiceContainer Proxy(this IServiceContainer self, Type iface, string? name, Func<IInjector, Type, object, object> decorator)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));
            Ensure.Parameter.IsNotNull(decorator, nameof(decorator));

            //
            // "QueryModes.ThrowOnError" miatt "entry" sose NULL
            //

            AbstractServiceEntry entry = self.Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError)!;

            if (entry.Owner != self)
                throw new InvalidOperationException(Resources.CANT_PROXY);

            entry.ApplyProxy(decorator);

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
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <typeparam name="TInterface">The service to be decorated.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 2nd parameter of the decorator function.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see above).</exception>
        public static IServiceContainer Proxy<TInterface>(this IServiceContainer self, string? name, Func<IInjector, TInterface, TInterface> decorator) where TInterface : class
            => self.Proxy(typeof(TInterface), name, (injector, type, instance) => decorator(injector, (TInterface) instance));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <typeparam name="TInterface">The service to be decorated.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 2nd parameter of the decorator function.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see above).</exception>
        public static IServiceContainer Proxy<TInterface>(this IServiceContainer self, Func<IInjector, TInterface, TInterface> decorator) where TInterface : class
            => self.Proxy(typeof(TInterface), null, (injector, type, instance) => decorator(injector, (TInterface)instance));

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
        public static IServiceContainer Proxy<TInterface, TInterceptor>(this IServiceContainer self, string? name = null) where TInterface: class where TInterceptor: InterfaceInterceptor<TInterface> => self.Proxy<TInterface>(name, (injector, instance) 
            => ProxyFactory.Create<TInterface, TInterceptor>(instance, injector));

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
        public static IServiceContainer Proxy(this IServiceContainer self, Type iface, string? name, Type interceptor) 
            => self.Proxy(iface, name, (injector, type, instance) => ProxyFactory.Create(iface, interceptor, instance, injector));

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
    }
}