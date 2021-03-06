/********************************************************************************
* Service.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

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
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory(IServiceContainer, Type, Func{IInjector, Type, object}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceContainer Service(this IServiceContainer self, Type iface, string? name, Type implementation, Lifetime lifetime)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            //
            // Tobbi parametert az xXxServiceEntry konstruktora fogja ellenorizni.
            //

            self.Add(lifetime.CreateFrom(iface, name, implementation, self));
            return self;
        }

        /// <summary>
        /// Registers a new service with the given implementation.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory(IServiceContainer, Type, Func{IInjector, Type, object}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceContainer Service(this IServiceContainer self, Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, Lifetime lifetime)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            if (explicitArgs is null)
                throw new ArgumentNullException(nameof(explicitArgs));

            //
            // Tobbi parametert az xXxServiceEntry konstruktora fogja ellenorizni.
            //

            self.Add(lifetime.CreateFrom(iface, name, implementation, explicitArgs, self));
            return self;
        }

        /// <summary>
        /// Registers a new service with the given implementation.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory(IServiceContainer, Type, Func{IInjector, Type, object}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceContainer Service(this IServiceContainer self, Type iface, Type implementation, Lifetime lifetime) 
            => self.Service(iface, null, implementation, lifetime);

        /// <summary>
        /// Registers a new service with the given implementation.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The service implementation to be registered. It can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory(IServiceContainer, Type, Func{IInjector, Type, object}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where both the interface and the implementation are open generic types). The system will specialize the implementation if you request the concrete service.</remarks> 
        public static IServiceContainer Service(this IServiceContainer self, Type iface, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, Lifetime lifetime)
            => self.Service(iface, null, implementation, explicitArgs, lifetime);

        /// <summary>
        /// Registers a new service.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory{TInterface}(IServiceContainer, Func{IInjector, TInterface}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Service<TInterface, TImplementation>(this IServiceContainer self, Lifetime lifetime) where TInterface : class where TImplementation: TInterface 
            => self.Service(typeof(TInterface), typeof(TImplementation), lifetime);

        /// <summary>
        /// Registers a new service.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory{TInterface}(IServiceContainer, Func{IInjector, TInterface}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Service<TInterface, TImplementation>(this IServiceContainer self, IReadOnlyDictionary<string, object?> explicitArgs, Lifetime lifetime) where TInterface : class where TImplementation : TInterface
            => self.Service(typeof(TInterface), typeof(TImplementation), explicitArgs, lifetime);

        /// <summary>
        /// Registers a new service.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory{TInterface}(IServiceContainer, Func{IInjector, TInterface}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Service<TInterface, TImplementation>(this IServiceContainer self, string name, Lifetime lifetime) where TInterface : class where TImplementation : TInterface 
            => self.Service(typeof(TInterface), name, typeof(TImplementation), lifetime);

        /// <summary>
        /// Registers a new service.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <typeparam name="TImplementation">The service implementation to be registered. It must implement the <typeparamref name="TInterface"/> interface and must have only null or one constructor (that may request another dependecies). In case of multiple constructors you can use the <see cref="IServiceContainerBasicExtensions.Factory{TInterface}(IServiceContainer, Func{IInjector, TInterface}, Lifetime)"/> method or the <see cref="ServiceActivatorAttribute"/>.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Service<TInterface, TImplementation>(this IServiceContainer self, string name, IReadOnlyDictionary<string, object?> explicitArgs, Lifetime lifetime) where TInterface : class where TImplementation : TInterface
            => self.Service(typeof(TInterface), name, typeof(TImplementation), explicitArgs, lifetime);
    }
}