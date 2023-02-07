/********************************************************************************
* Factory.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

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
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You can register generic services (where the <paramref name="iface"/> parameter is an open generic type).</remarks>
        public static IServiceCollection Factory(this IServiceCollection self, Type iface, string? name, Expression<FactoryDelegate> factory, LifetimeBase lifetime, ServiceOptions? options = null)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            if (lifetime is null)
                throw new ArgumentNullException(nameof(lifetime));

            return self.Register
            (
                //
                // Further validations are done by the created xXxServiceEntry
                //

                lifetime.CreateFrom(iface, name, factory, options ?? ServiceOptions.Default)
            );
        }

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter. Note that the second parameter of the <paramref name="factory"/> is never generic, even if you registered the factory for an open generic interface.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        /// <remarks>You can register generic services (where the <paramref name="iface"/> parameter is an open generic type).</remarks>
        public static IServiceCollection Factory(this IServiceCollection self, Type iface, Expression<FactoryDelegate> factory, LifetimeBase lifetime, ServiceOptions? options = null) 
            => self.Factory(iface, null, factory, lifetime, options);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Factory<TInterface>(this IServiceCollection self, string? name, Expression<FactoryDelegate<TInterface>> factory, LifetimeBase lifetime, ServiceOptions? options = null) where TInterface : class
            => self.Factory(typeof(TInterface), name, WrapToStandardDelegate(factory), lifetime, options);

        /// <summary>
        /// Registers a new service factory with the given type. Factories are also services except that the instantiating process is delegated to the caller. Useful if a service has more than one constructor.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="factory">The factory function that is responsible for the instantiation. Its call count depends on the value of the <paramref name="lifetime"/> parameter.</param>
        /// <param name="lifetime">The lifetime of service.</param>
        /// <param name="options">Options to be assigned to the service being registered.</param>
        public static IServiceCollection Factory<TInterface>(this IServiceCollection self, Expression<FactoryDelegate<TInterface>> factory, LifetimeBase lifetime, ServiceOptions? options = null) where TInterface : class
            => self.Factory(typeof(TInterface), null, WrapToStandardDelegate(factory), lifetime, options);

        private static Expression<FactoryDelegate> WrapToStandardDelegate<TInterface>(Expression<FactoryDelegate<TInterface>> factory) where TInterface : class
        {
            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            ParameterExpression
                injector = factory.Parameters[0],
                iface = Expression.Parameter(typeof(Type), nameof(iface));

            return Expression.Lambda<FactoryDelegate>
            (
                factory.Body,
                injector,
                iface
            );
        }
    }
}