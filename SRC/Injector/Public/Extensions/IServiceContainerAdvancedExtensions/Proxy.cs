/********************************************************************************
* Proxy.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Interfaces.Properties;
    using Internals;
    using Proxy;

    public static partial class IServiceContainerAdvancedExtensions
    {
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
        public static IServiceContainer Proxy<TInterface, TInterceptor>(this IServiceContainer self, string? name = null) where TInterface: class where TInterceptor: InterfaceInterceptor<TInterface> => self.Proxy(typeof(TInterface), name, typeof(TInterceptor));

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
        {
            //
            // A tobbit a ProxyGenerator<> ellenorzi
            //

            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (interceptor == null)
                throw new ArgumentNullException(nameof(interceptor));

            AbstractServiceEntry entry = self.Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError)!;

            if (entry.Owner != self)
                throw new InvalidOperationException(Resources.INAPROPRIATE_OWNERSHIP);

            entry.ApplyInterceptor(interceptor);

            return self;
        }

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