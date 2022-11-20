/********************************************************************************
* Provider.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;

    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The interface of the service. It will be forwarded to the provider.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IModifiedServiceCollection Provider(this IServiceCollection self, Type iface, string? name, Type provider, object explicitArgs, LifetimeBase lifetime)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

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

            if (iface.IsGenericTypeDefinition)
                throw new NotSupportedException(Resources.OPEN_GENERIC);

            return self
                .Service(iface, name, provider, explicitArgs, lifetime)
                .UsingProxy((injector, iface, instance) => ((IServiceProvider) instance).GetService(iface));
        }

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The interface of the service. It will be forwarded to the provider.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IModifiedServiceCollection Provider(this IServiceCollection self, Type iface, string? name, Type provider, LifetimeBase lifetime)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (provider is null)
                throw new ArgumentNullException(nameof(provider));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            //
            // Further validations are done by the created xXxServiceEntry
            //

            if (!typeof(IServiceProvider).IsAssignableFrom(provider))
                throw new ArgumentException(string.Format(Resources.Culture, Resources.NOT_IMPLEMENTED, typeof(IServiceProvider)), nameof(provider));

            if (iface.IsGenericTypeDefinition)
                throw new NotSupportedException(Resources.OPEN_GENERIC);

            return self
                .Service(iface, name, provider, lifetime)
                .UsingProxy((injector, iface, instance) => ((IServiceProvider) instance).GetService(iface));
        }

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The interface of the service. It will be forwarded to the provider.</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IModifiedServiceCollection Provider(this IServiceCollection self, Type iface, Type provider, LifetimeBase lifetime) => self.Provider(iface, null, provider, lifetime);

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The interface of the service. It will be forwarded to the provider.</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IModifiedServiceCollection Provider(this IServiceCollection self, Type iface, Type provider, object explicitArgs, LifetimeBase lifetime) => self.Provider(iface, null, provider, explicitArgs, lifetime);

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <typeparam name="TInterface">The interface of the service. It will be forwarded to the provider.</typeparam>
        /// <typeparam name="TProvider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IModifiedServiceCollection Provider<TInterface, TProvider>(this IServiceCollection self, string? name, LifetimeBase lifetime) where TProvider : class, IServiceProvider where TInterface : class
            => self.Provider(typeof(TInterface), name, typeof(TProvider), lifetime);

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <typeparam name="TInterface">The interface of the service. It will be forwarded to the provider.</typeparam>
        /// <typeparam name="TProvider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IModifiedServiceCollection Provider<TInterface, TProvider>(this IServiceCollection self, LifetimeBase lifetime) where TProvider : class, IServiceProvider where TInterface : class
            => self.Provider<TInterface, TProvider>(name: null, lifetime);

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <typeparam name="TInterface">The interface of the service. It will be forwarded to the provider.</typeparam>
        /// <typeparam name="TProvider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <remarks>The provided <see cref="IServiceProvider"/> implementation won't be disposed even if it implements the <see cref="IDisposable"/> interface.</remarks>
        public static IModifiedServiceCollection Provider<TInterface, TProvider>(this IServiceCollection self, object explicitArgs, LifetimeBase lifetime) where TProvider : class, IServiceProvider where TInterface : class
            => self.Provider(typeof(TInterface), typeof(TProvider), explicitArgs, lifetime);
    }
}