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
        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="interceptors">The interceptors to be applied.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// Using this method is the preferred way to apply multiple interceptors against the same service as it will only create one backing proxy.
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, params (Type Interceptor, object? ExplicitArg)[] interceptors)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (interceptors is null)
                throw new ArgumentNullException(nameof(interceptors));

            self.Last().ApplyInterceptors(interceptors);
            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="interceptors">The interceptors to be applied.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// Using this method is the preferred way to apply multiple interceptors against the same service as it will only create one backing proxy.
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, params Type[] interceptors)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (interceptors is null)
                throw new ArgumentNullException(nameof(interceptors));

            self.Last().ApplyInterceptors
            (
                interceptors.Select
                (
                    static i => (i, (object?) null)
                )
            );
            return self;
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
        public static IServiceCollection Decorate(this IServiceCollection self, Type interceptor, object? explicitArgs) =>
            self.Decorate(new[] { (interceptor, explicitArgs) });

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
            self.Decorate(new[] { interceptor });

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
            self.Decorate(new[] { typeof(TInterceptor) });

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="interceptors">Interceptors to be applied.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// Using this method is the preferred way to apply multiple interceptors against the same service as it will only create one backing proxy.
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type type, object? key, params (Type Interceptor, object? ExplicitArg)[] interceptors)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (interceptors is null)
                throw new ArgumentNullException(nameof(interceptors));

            self.Find(type, key).ApplyInterceptors(interceptors);
            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="interceptors">Interceptors to be applied.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// Using this method is the preferred way to apply multiple interceptors against the same service as it will only create one backing proxy.
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type type, object? key, params Type[] interceptors)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (interceptors is null)
                throw new ArgumentNullException(nameof(interceptors));

            self.Find(type, key).ApplyInterceptors
            (
                interceptors.Select
                (
                    static interceptor => (interceptor, (object?) null)
                )
            );
            return self;
        }

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
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
        public static IServiceCollection Decorate(this IServiceCollection self, Type type, object? key, Type interceptor, object? explicitArgs) =>
            self.Decorate(type, key, new[] { (interceptor, explicitArgs) });

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="interceptor">The interceptor type.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type type, object? key, Type interceptor) =>
            self.Decorate(type, key, new[] { interceptor });

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
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
        public static IServiceCollection Decorate(this IServiceCollection self, Type type, Type interceptor, object? explicitArgs) =>
            self.Decorate(type, null, interceptor, explicitArgs);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type.</param>
        /// <param name="interceptor">The interceptor type.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Type type, Type interceptor) =>
            self.Decorate(type, key: null, new[] { interceptor });

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="explicitArgs">Explicit arguments to be passed.</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate<TType, TInterceptor>(this IServiceCollection self, object? key, object? explicitArgs) where TType : class where TInterceptor : IInterfaceInterceptor
            => self.Decorate(typeof(TType), key, typeof(TInterceptor), explicitArgs);

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <remarks>
        /// <list type="bullet">
        /// <item>You can't create proxies against instances and open generic services.</item>
        /// <item>A service can be decorated multiple times.</item>
        /// <item>Wrapping a service into an interceptor implies that it cannot be disposed unless the service interface itself implements the <see cref="IDisposable"/>.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="NotSupportedException">When proxying is not allowed (see remarks).</exception>
        public static IServiceCollection Decorate<TType, TInterceptor>(this IServiceCollection self, object? key) where TType: class where TInterceptor: IInterfaceInterceptor
            => self.Decorate(typeof(TType), key, typeof(TInterceptor));

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
        public static IServiceCollection Decorate<TType, TInterceptor>(this IServiceCollection self) where TType : class where TInterceptor : IInterfaceInterceptor
            => self.Decorate(typeof(TType), typeof(TInterceptor));
    }
}