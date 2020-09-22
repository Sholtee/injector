/********************************************************************************
* Factory.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    public static partial class IServiceContainerBasicExtensions
    {
        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name  of the service.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="entryFactory"/> parameter. Note that the second parameter of the <paramref name="factory"/> is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="entryFactory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can register generic services (where the <paramref name="iface"/> parameter is an open generic type).</remarks>
        public static IServiceContainer Factory(this IServiceContainer self, Type iface, string? name, Func<IInjector, Type, object> factory, IServiceEntryFactory entryFactory)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (entryFactory == null)
                throw new ArgumentNullException(nameof(entryFactory));

            //
            // Tobbi parametert az xXxServiceEntry konstruktora fogja ellenorizni.
            //

            self.Add(entryFactory.CreateFrom(iface, name, factory, self));
            return self;
        }

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="entryFactory"/> parameter. Note that the second parameter of the <paramref name="factory"/> is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="entryFactory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can register generic services (where the <paramref name="iface"/> parameter is an open generic type).</remarks>
        public static IServiceContainer Factory(this IServiceContainer self, Type iface, Func<IInjector, Type, object> factory, IServiceEntryFactory entryFactory) 
            => self.Factory(iface, null, factory, entryFactory);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="entryFactory"/> parameter.</param>
        /// <param name="entryFactory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Factory<TInterface>(this IServiceContainer self, string? name, Func<IInjector, TInterface> factory, IServiceEntryFactory entryFactory) where TInterface : class
            => self.Factory(typeof(TInterface), name, (injector, type) => factory(injector), entryFactory);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="entryFactory"/> parameter.</param>
        /// <param name="entryFactory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Factory<TInterface>(this IServiceContainer self, Func<IInjector, TInterface> factory, IServiceEntryFactory entryFactory) where TInterface : class
            => self.Factory(null, injector => factory(injector), entryFactory);
    }
}