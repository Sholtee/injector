/********************************************************************************
* Provider.cs                                                                   *
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
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The interface of the service. It will be forwarded to the provider.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Provider(this IServiceContainer self, Type iface, string? name, Type provider, Lifetime lifetime)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (provider is null)
                throw new ArgumentNullException(nameof(self));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            //
            // Tovabbi validaciot az xXxServiceEntry vegzi.
            //

            if (!typeof(IServiceProvider).IsAssignableFrom(provider))
                throw new ArgumentException(string.Format(Resources.Culture, Resources.NOT_IMPLEMENTED, typeof(IServiceProvider)), nameof(provider));

            //
            // Ez jol kezeli azt az esetet is ha eloszor generikus majd pedig annak lezart parjat regisztraljuk
            //

            string providerName = $"${iface.FullName}_provider_{name}"; // TODO: INTERNAL_SERVICE_NAME_PREFIX hasznalata

            return self
                //
                // A legyartott szerviz elettartama nem feltetlen kell megegyezzen a mogottes provider elettartamaval
                // (hogy peldaul Lifetime.Pooled eseten ne legyen egy felesleges pool peldany is).
                //

                .Service(typeof(IServiceProvider), providerName, provider, lifetime.CompareTo(Lifetime.Singleton) < 0 ? Lifetime.Transient : lifetime)
                .Factory(iface, name, (injector, iface) => injector.Get<IServiceProvider>(providerName).GetService(iface), lifetime);
        }

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The interface of the service. It will be forwarded to the provider.</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Provider(this IServiceContainer self, Type iface, Type provider, Lifetime lifetime) => self.Provider(iface, null, provider, lifetime);

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <typeparam name="TInterface">The interface of the service. It will be forwarded to the provider.</typeparam>
        /// <typeparam name="TProvider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Provider<TInterface, TProvider>(this IServiceContainer self, string? name, Lifetime lifetime) where TProvider : class, IServiceProvider where TInterface : class
            => self.Provider(typeof(TInterface), name, typeof(TProvider), lifetime);

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <typeparam name="TInterface">The interface of the service. It will be forwarded to the provider.</typeparam>
        /// <typeparam name="TProvider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Provider<TInterface, TProvider>(this IServiceContainer self, Lifetime lifetime) where TProvider : class, IServiceProvider where TInterface : class
            => self.Provider<TInterface, TProvider>(null, lifetime);
    }
}