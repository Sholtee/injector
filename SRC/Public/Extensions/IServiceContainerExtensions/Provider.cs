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
        /// <returns></returns>
        public static IServiceContainer Provider<TInterface, TProvider>(this IServiceContainer self, string? name, Lifetime lifetime) where TProvider: class, IServiceProvider where TInterface: class
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            Func<IInjector, Type, object> providerFactory = Resolver.Get(typeof(TProvider));

            return self.Factory(name, GetService, lifetime);

            TInterface GetService(IInjector injector) 
            {
                IServiceProvider provider = (IServiceProvider) providerFactory.Invoke(injector, typeof(IServiceProvider));
                return (TInterface) provider.GetService(typeof(TInterface));
            }
        }
    }
}