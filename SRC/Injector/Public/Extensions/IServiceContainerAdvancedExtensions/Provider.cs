/********************************************************************************
* Provider.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;
    using Properties;

    public static partial class IServiceContainerAdvancedExtensions
    {
        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The interface of the service. It will be forwarded to the provider.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="entryFactory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Provider(this IServiceContainer self, Type iface, string? name, Type provider, IServiceEntryFactory entryFactory)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));
            Ensure.Parameter.IsNotNull(provider, nameof(provider));

            if (!IsProvider(provider))
                throw new ArgumentException(string.Format(Resources.Culture, Interfaces.Properties.Resources.INTERFACE_NOT_SUPPORTED, typeof(IServiceProvider)), nameof(provider));

            //
            // Ezeket a Factory() hivas ellenorizne, itt csak azert van h ne legyen
            // felesleges Resolver.Get() hivas.
            //

            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));

            //
            // A "Resolver.Get()" hivas validal is
            //

            Func<IInjector, Type, object> providerFactory = Resolver.Get(provider);

            return self.Factory(iface, name, GetService, entryFactory);

            object GetService(IInjector injector, Type iface)
            {
                IServiceProvider provider = (IServiceProvider) providerFactory.Invoke(injector, typeof(IServiceProvider));

                //
                // Nem gond ha NULL-t v rossz tipusu peldanyt ad vissza mert az injector validalni fogja.
                //

                return provider.GetService(iface);
            }

            static bool IsProvider(Type type)
            {
                foreach (Type iface in type.GetInterfaces())
                {
                    if (iface == typeof(IServiceProvider) || IsProvider(iface))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The interface of the service. It will be forwarded to the provider.</param>
        /// <param name="provider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</param>
        /// <param name="entryFactory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Provider(this IServiceContainer self, Type iface, Type provider, IServiceEntryFactory entryFactory) => self.Provider(iface, null, provider, entryFactory);

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <typeparam name="TInterface">The interface of the service. It will be forwarded to the provider.</typeparam>
        /// <typeparam name="TProvider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="entryFactory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Provider<TInterface, TProvider>(this IServiceContainer self, string? name, IServiceEntryFactory entryFactory) where TProvider : class, IServiceProvider where TInterface : class
            => self.Provider(typeof(TInterface), name, typeof(TProvider), entryFactory);

        /// <summary>
        /// Registers a new provider. Providers are "factory services" responsible for creating the concrete service.
        /// </summary>
        /// <typeparam name="TInterface">The interface of the service. It will be forwarded to the provider.</typeparam>
        /// <typeparam name="TProvider">The type of the provider. It may have dependencies and must implement the <see cref="IServiceProvider"/> interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="entryFactory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Provider<TInterface, TProvider>(this IServiceContainer self, IServiceEntryFactory entryFactory) where TProvider : class, IServiceProvider where TInterface : class
            => self.Provider<TInterface, TProvider>(null, entryFactory);
    }
}