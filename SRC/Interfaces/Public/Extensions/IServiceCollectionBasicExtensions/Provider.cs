/********************************************************************************
* Provider.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;

    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Registers a new service provider. Providers are "factory services" responsible for creating the concrete service:
        /// <code>
        /// class MyServiceProvider: IServiceProvider
        /// {
        ///     public int Depdendency { get; init; }
        ///     public object GetService(Type type) => ...;
        /// }
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Provider(typeof(IMyService), "serviceName", typeof(MyServiceProvider), new { Depdendency = 1986 }, Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type. This parameter will be forwarded to the provider.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IServiceCollection Provider(this IServiceCollection self, Type type, object? key, Type provider, object explicitArgs, LifetimeBase lifetime, ServiceOptions? options = null)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

            if (explicitArgs is null)
                throw new ArgumentNullException(nameof(explicitArgs));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            //
            // Further validations are done by the created xXxServiceEntry
            //

            if (!typeof(IServiceProvider).IsAssignableFrom(provider))
                throw new ArgumentException(string.Format(Resources.Culture, Resources.NOT_IMPLEMENTED, typeof(IServiceProvider)), nameof(provider));

            if (type.IsGenericTypeDefinition)
                throw new NotSupportedException(Resources.OPEN_GENERIC);

            return self
                .Service(type, key, provider, explicitArgs, lifetime, options)
                .Decorate(static (injector, type, instance) => ((IServiceProvider) instance).GetService(type));
        }

        /// <summary>
        /// Registers a new service provider. Providers are "factory services" responsible for creating the concrete service.
        /// <code>
        /// class MyServiceProvider: IServiceProvider
        /// {
        ///     [Inject]
        ///     public IDependency Depdendency { get; init; }
        ///     public object GetService(Type type) => ...;
        /// }
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Provider(typeof(IMyService), "serviceName", typeof(MyServiceProvider), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type. This parameter will be forwarded to the provider.</param>
        /// <param name="key">The (optional) service key (usually a name).</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IServiceCollection Provider(this IServiceCollection self, Type type, object? key, Type provider, LifetimeBase lifetime, ServiceOptions? options = null)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            //
            // Further validations are done by the created xXxServiceEntry
            //

            if (!typeof(IServiceProvider).IsAssignableFrom(provider))
                throw new ArgumentException(string.Format(Resources.Culture, Resources.NOT_IMPLEMENTED, typeof(IServiceProvider)), nameof(provider));

            if (type.IsGenericTypeDefinition)
                throw new NotSupportedException(Resources.OPEN_GENERIC);

            return self
                .Service(type, key, provider, lifetime, options)
                .Decorate(static (injector, type, instance) => ((IServiceProvider) instance).GetService(type));
        }

        /// <summary>
        /// Registers a new service provider. Providers are "factory services" responsible for creating the concrete service:
        /// <code>
        /// class MyServiceProvider: IServiceProvider
        /// {
        ///     [Inject]
        ///     public IDependency Depdendency { get; init; }
        ///     public object GetService(Type type) => ...;
        /// }
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Provider(typeof(IMyService), typeof(MyServiceProvider), Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type. This parameter will be forwarded to the provider.</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IServiceCollection Provider(this IServiceCollection self, Type type, Type provider, LifetimeBase lifetime, ServiceOptions? options = null)
            => self.Provider(type, key: null, provider, lifetime, options);

        /// <summary>
        /// Registers a new service provider having non-interface dependency. Providers are "factory services" responsible for creating the concrete service:
        /// <code>
        /// class MyServiceProvider: IServiceProvider
        /// {
        ///     public int Depdendency { get; init; }
        ///     public object GetService(Type type) => ...;
        /// }
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Provider(typeof(IMyService), typeof(MyServiceProvider), new { Depdendency = 1986 }, Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="type">The service type. This parameter will be forwarded to the provider.</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IServiceCollection Provider(this IServiceCollection self, Type type, Type provider, object explicitArgs, LifetimeBase lifetime, ServiceOptions? options = null)
            => self.Provider(type, null, provider, explicitArgs, lifetime, options);

        /// <summary>
        /// Registers a new service provider. Providers are "factory services" responsible for creating the concrete service:
        /// <code>
        /// class MyServiceProvider: IServiceProvider
        /// {
        ///     [Inject]
        ///     public IDependency Depdendency { get; init; }
        ///     public object GetService(Type type) => ...;
        /// }
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Provider&lt;IMyService, MyServiceProvider&gt;("serviceName", Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TType">The service type. It will be forwarded to the provider.</typeparam>
        /// <typeparam name="TProvider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name)</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IServiceCollection Provider<TType, TProvider>(this IServiceCollection self, object? key, LifetimeBase lifetime, ServiceOptions? options = null) where TProvider : class, IServiceProvider where TType : class
            => self.Provider(typeof(TType), key, typeof(TProvider), lifetime, options);

        /// <summary>
        /// Registers a new service provider. Providers are "factory services" responsible for creating the concrete service:
        /// <code>
        /// class MyServiceProvider: IServiceProvider
        /// {
        ///     [Inject]
        ///     public IDependency Depdendency { get; init; }
        ///     public object GetService(Type type) => ...;
        /// }
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Provider&lt;IMyService, MyServiceProvider&gt;(Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TType">The service type. It will be forwarded to the provider.</typeparam>
        /// <typeparam name="TProvider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IServiceCollection Provider<TType, TProvider>(this IServiceCollection self, LifetimeBase lifetime, ServiceOptions? options = null) where TProvider : class, IServiceProvider where TType : class
            => self.Provider<TType, TProvider>(key: null, lifetime, options);

        /// <summary>
        /// Registers a new service provider having non-interface dependency. Providers are "factory services" responsible for creating the concrete service:
        /// <code>
        /// class MyServiceProvider: IServiceProvider
        /// {
        ///     public int Depdendency { get; init; }
        ///     public object GetService(Type type) => ...;
        /// }
        /// ...
        /// ScopeFactory.Create
        /// (
        ///     svcs => svcs.Provider&lt;IMyService, MyServiceProvider&gt;("serviceName", new { Depdendency = 1986 }, Lifetime.Singleton),
        ///     ...
        /// )
        /// </code>
        /// </summary>
        /// <typeparam name="TType">The service type. It will be forwarded to the provider.</typeparam>
        /// <typeparam name="TProvider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="key">The (optional) service key (usually a name)</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IServiceCollection Provider<TType, TProvider>(this IServiceCollection self, object? key, object explicitArgs, LifetimeBase lifetime, ServiceOptions? options = null) where TProvider : class, IServiceProvider where TType : class
            => self.Provider(typeof(TType), key, typeof(TProvider), explicitArgs, lifetime, options);
    }
}