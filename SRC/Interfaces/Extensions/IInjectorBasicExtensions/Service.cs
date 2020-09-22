/********************************************************************************
* Service.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    public static partial class IServiceContainerBasicExtensions
    {
        /// <summary>
        /// Registers a new service with the given implementation.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory(IServiceContainer, Type, Func{IInjector, Type, object}, IServiceEntryFactory)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="factory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceContainer Service(this IServiceContainer self, Type iface, string? name, Type implementation, IServiceEntryFactory factory)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            //
            // Tobbi parametert az xXxServiceEntry konstruktora fogja ellenorizni.
            //

            self.Add(factory.CreateFrom(iface, name, implementation, self));
            return self;
        }

        /// <summary>
        /// Registers a new service with the given implementation.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory(IServiceContainer, Type, Func{IInjector, Type, object}, IServiceEntryFactory)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="factory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceContainer Service(this IServiceContainer self, Type iface, Type implementation, IServiceEntryFactory factory) 
            => self.Service(iface, null, implementation, factory);

        /// <summary>
        /// Registers a new service.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory{TInterface}(IServiceContainer, Func{IInjector, TInterface}, IServiceEntryFactory)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="factory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Service<TInterface, TImplementation>(this IServiceContainer self, IServiceEntryFactory factory) where TInterface : class where TImplementation: TInterface 
            => self.Service(typeof(TInterface), typeof(TImplementation), factory);

        /// <summary>
        /// Registers a new service.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory{TInterface}(IServiceContainer, Func{IInjector, TInterface}, IServiceEntryFactory)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="factory">The service entry factory.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Service<TInterface, TImplementation>(this IServiceContainer self, string name, IServiceEntryFactory factory) where TInterface : class where TImplementation : TInterface 
            => self.Service(typeof(TInterface), name, typeof(TImplementation), factory);
    }
}