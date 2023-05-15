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

    public static partial class IServiceCollectionAdvancedExtensions
    {
        private static void Decorate(this AbstractServiceEntry entry, Type interceptor, object? explicitArgs)
        {
            if (entry is null)
                throw new ArgumentNullException(nameof(entry));

            ServiceOptions options = entry.Options ?? ServiceOptions.Default;

            entry.Decorate
            (
                DecoratorResolver.Resolve
                (
                    entry.Interface,

                    //
                    // Proxies registered by this way always target the service interface.
                    //

                    entry.Interface,
                    options.ProxyEngine,
                    DecoratorResolver.ResolveInterceptorFactory(interceptor, explicitArgs, options.DependencyResolvers)
                )
            );
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="interceptor">The interceptor type.</param>
        /// <param name="explicitArgs">Explicit arguments to be passed.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type interceptor, object? explicitArgs)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (interceptor is null)
                throw new ArgumentNullException(nameof(interceptor));

            self.Last().Decorate(interceptor, explicitArgs);
            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="interceptor">The interceptor type.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type interceptor) =>
            self.Decorate(interceptor, explicitArgs: null);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
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
        /// <param name="explicitArgs">Explicit arguments to be passed.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type iface, string? name, Type interceptor, object? explicitArgs)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (interceptor is null)
                throw new ArgumentNullException(nameof(interceptor));

            self.Find(iface, name).Decorate(interceptor, explicitArgs);
            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <param name="name">The (optional) service name.</param>
        /// <param name="interceptor">The interceptor type.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type iface, string? name, Type interceptor) =>
            self.Decorate(iface, name, interceptor, null);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <param name="interceptor">The interceptor type.</param>
        /// <param name="explicitArgs">Explicit arguments to be passed.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type iface, Type interceptor, object? explicitArgs) =>
            self.Decorate(iface, null, interceptor, explicitArgs);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <param name="interceptor">The interceptor type.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type iface, Type interceptor) =>
            self.Decorate(iface, null, interceptor, null);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) service name.</param>
        /// <param name="explicitArgs">Explicit arguments to be passed.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate<TInterface, TInterceptor>(this IServiceCollection self, string? name, object? explicitArgs) where TInterface : class where TInterceptor : IInterfaceInterceptor
            => self.Decorate(typeof(TInterface), name, typeof(TInterceptor), explicitArgs);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) service name.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate<TInterface, TInterceptor>(this IServiceCollection self, string? name) where TInterface: class where TInterceptor: IInterfaceInterceptor
            => self.Decorate(typeof(TInterface), name, typeof(TInterceptor));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="explicitArgs">Explicit arguments to be passed.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate<TInterface, TInterceptor>(this IServiceCollection self, object? explicitArgs) where TInterface : class where TInterceptor : IInterfaceInterceptor
            => self.Decorate(typeof(TInterface), typeof(TInterceptor), explicitArgs);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate<TInterface, TInterceptor>(this IServiceCollection self) where TInterface : class where TInterceptor : IInterfaceInterceptor
            => self.Decorate(typeof(TInterface), typeof(TInterceptor), null);
    }
}