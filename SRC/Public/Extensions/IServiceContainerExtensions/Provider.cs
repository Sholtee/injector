/********************************************************************************
* Provider.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Internals;

    public static partial class IServiceContainerExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="iface"></param>
        /// <param name="name"></param>
        /// <param name="provider"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IServiceContainer Provider(this IServiceContainer self, Type iface, string? name, Type provider, Lifetime lifetime = Lifetime.Transient)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            Ensure.Parameter.IsNotNull(provider, nameof(provider));
            Ensure.Type.Supports(provider, typeof(IServiceProvider));

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

            return self.Factory(iface, name, GetService, lifetime);

            object GetService(IInjector injector, Type iface)
            {
                IServiceProvider provider = (IServiceProvider) providerFactory.Invoke(injector, typeof(IServiceProvider));

                //
                // Nem gond ha NULL-t v rossz tipusu peldanyt ad vissza mert az injector validalni fogja.
                //

                return provider.GetService(iface);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="iface"></param>
        /// <param name="provider"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IServiceContainer Provider(this IServiceContainer self, Type iface, Type provider, Lifetime lifetime = Lifetime.Transient) => self.Provider(iface, null, provider, lifetime);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TProvider"></typeparam>
        /// <param name="self"></param>
        /// <param name="name"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IServiceContainer Provider<TInterface, TProvider>(this IServiceContainer self, string? name, Lifetime lifetime = Lifetime.Transient) where TProvider : class, IServiceProvider where TInterface : class
            => self.Provider(typeof(TInterface), name, typeof(TProvider), lifetime);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TProvider"></typeparam>
        /// <param name="self"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public static IServiceContainer Provider<TInterface, TProvider>(this IServiceContainer self, Lifetime lifetime = Lifetime.Transient) where TProvider : class, IServiceProvider where TInterface : class
            => self.Provider<TInterface, TProvider>(null, lifetime);
    }
}