/********************************************************************************
* Decorate.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    using static Properties.Resources;
    using static Interfaces.Properties.Resources;

    public static partial class IServiceCollectionAdvancedExtensions
    {
        private static void Decorate(this AbstractServiceEntry entry, Type interceptor)
        {
            if (!typeof(IInterfaceInterceptor).IsAssignableFrom(interceptor))
                throw new ArgumentException(NOT_AN_INTERCEPTOR, nameof(interceptor));

            if (entry is not ProducibleServiceEntry pse)
                throw new NotSupportedException(DECORATING_NOT_SUPPORTED);

            pse.Decorate
            (
                //
                // Proxies registered by this way always target the service interface.
                //

                new DecoratorResolver(pse.Options.DependencyResolvers).Resolve
                (
                    pse.Interface,
                    pse.Interface,
                    new Type[] { interceptor },
                    pse.Options.ProxyEngine ?? ProxyEngine.Instance
                )
            );
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="interceptor">The interceptor type.</param>
        /// <remarks>You can't create proxies against instances and open generic services. A service can be decorated multiple times.</remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type interceptor)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (interceptor is null)
                throw new ArgumentNullException(nameof(interceptor));

            self.Last().Decorate(interceptor);
            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <remarks>You can't create proxies against instances and open generic services. A service can be decorated multiple times.</remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate<TInterceptor>(this IServiceCollection self) where TInterceptor : IInterfaceInterceptor =>
            self.Decorate(typeof(TInterceptor));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <param name="name">The (optional) service name.</param>
        /// <param name="interceptor">The interceptor type.</param>
        /// <remarks>You can't create proxies against instances and open generic services. A service can be decorated multiple times.</remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type iface, string? name, Type interceptor)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (interceptor is null)
                throw new ArgumentNullException(nameof(interceptor));

            self.Find(iface, name).Decorate(interceptor);
            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <param name="interceptor">The interceptor type.</param>
        /// <remarks>You can't create proxies against instances and open generic services. A service can be decorated multiple times.</remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type iface, Type interceptor) =>
            self.Decorate(iface, null, interceptor);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) service name.</param>
        /// <remarks>You can't create proxies against instances and open generic services. A service can be decorated multiple times.</remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate<TInterface, TInterceptor>(this IServiceCollection self, string? name) where TInterface: class where TInterceptor: IInterfaceInterceptor
            => self.Decorate(typeof(TInterface), name, typeof(TInterceptor));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <remarks>You can't create proxies against instances and open generic services. A service can be decorated multiple times.</remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate<TInterface, TInterceptor>(this IServiceCollection self) where TInterface : class where TInterceptor : IInterfaceInterceptor
            => self.Decorate(typeof(TInterface), typeof(TInterceptor));
    }
}