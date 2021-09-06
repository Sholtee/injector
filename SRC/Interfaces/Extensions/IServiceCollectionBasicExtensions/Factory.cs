/********************************************************************************
* Factory.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name  of the service.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter. Note that the second parameter of the <paramref name="factory"/> is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <remarks>You can register generic services (where the <paramref name="iface"/> parameter is an open generic type).</remarks>
        public static IModifiedServiceCollection Factory(this IServiceCollection self, Type iface, string? name, Func<IInjector, Type, object> factory, Lifetime lifetime)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (lifetime == null)
                throw new ArgumentNullException(nameof(lifetime));

            //
            // Tobbi parametert az xXxServiceEntry konstruktora fogja ellenorizni.
            //

            return self.Register
            (
                lifetime.CreateFrom(iface, name, factory)
            );
        }

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter. Note that the second parameter of the <paramref name="factory"/> is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <remarks>You can register generic services (where the <paramref name="iface"/> parameter is an open generic type).</remarks>
        public static IModifiedServiceCollection Factory(this IServiceCollection self, Type iface, Func<IInjector, Type, object> factory, Lifetime lifetime) 
            => self.Factory(iface, null, factory, lifetime);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public static IModifiedServiceCollection Factory<TInterface>(this IServiceCollection self, string? name, Func<IInjector, TInterface> factory, Lifetime lifetime) where TInterface : class
            => self.Factory(typeof(TInterface), name, (injector, type) => factory(injector), lifetime);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public static IModifiedServiceCollection Factory<TInterface>(this IServiceCollection self, Func<IInjector, TInterface> factory, Lifetime lifetime) where TInterface : class
            => self.Factory(null, injector => factory(injector), lifetime);
    }
}