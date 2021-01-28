/********************************************************************************
* Proxy.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;
 
    public static partial class IServiceContainerBasicExtensions
    {
        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
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
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (decorator == null)
                throw new ArgumentNullException(nameof(decorator));

            //
            // - Get() validalja az "iface" parametert
            // - "QueryModes.ThrowOnError" miatt "entry" sose NULL
            //

            AbstractServiceEntry entry = self.Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError)!;

            if (entry.Owner != self)
                throw new InvalidOperationException(Resources.INAPROPRIATE_OWNERSHIP);

            if (entry is not ISupportsProxying setter || setter.Factory == null)
                //
                // Generikus szerviz, Abstract(), Instance() eseten a metodus nem ertelmezett.
                //

                throw new InvalidOperationException(Resources.PROXYING_NOT_SUPPORTED);

            //
            // Bovitjuk a hivasi lancot a decorator-al.
            //

            Func<IInjector, Type, object> oldFactory = setter.Factory;

            setter.Factory = (injector, type) => decorator(injector, type, oldFactory(injector, type));

            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service to be decorated.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of the decorator function.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against generic, instance or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceContainer Proxy(this IServiceContainer self, Type iface, Func<IInjector, Type, object, object> decorator) => self.Proxy(iface, null, decorator);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
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
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <typeparam name="TInterface">The service to be decorated.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 2nd parameter of the decorator function.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see above).</exception>
        public static IServiceContainer Proxy<TInterface>(this IServiceContainer self, Func<IInjector, TInterface, TInterface> decorator) where TInterface : class
            => self.Proxy(typeof(TInterface), null, (injector, type, instance) => decorator(injector, (TInterface) instance));
    }
}