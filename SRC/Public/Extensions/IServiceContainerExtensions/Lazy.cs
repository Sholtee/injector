/********************************************************************************
* Lazy.cs                                                                       *
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
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The <see cref="ITypeResolver"/> is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter. For an implementation see the <see cref="LazyTypeResolver{TInterface}"/> class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where the <paramref name="iface"/> parameter is an open generic <see cref="Type"/>). In this case the resolver must return an open generic implementation.</remarks>
        public static IServiceContainer Lazy(this IServiceContainer self, Type iface, string? name, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            //
            // Tobbi parametert az xXxServiceEntry konstruktora fogja ellenorizni.
            //

            self.Add(ProducibleServiceEntry.Create(lifetime, iface, name, implementation, self));
            return self;
        }

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="implementation">The <see cref="ITypeResolver"/> is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <paramref name="iface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter. For an implementation see the <see cref="LazyTypeResolver{TInterface}"/> class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You may register generic services (where the <paramref name="iface"/> parameter is an open generic <see cref="Type"/>). In this case the resolver must return an open generic implementation.</remarks>
        public static IServiceContainer Lazy(this IServiceContainer self, Type iface, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient)
            => self.Lazy(iface, null, implementation, lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request. Type resolution is done by the <see cref="LazyTypeResolver"/>.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once (with the given <paramref name="name"/>).</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="asmPath">The absolute path of the containing <see cref="System.Reflection.Assembly"/>.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <paramref name="iface"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy(this IServiceContainer self, Type iface, string? name, string asmPath, string className, Lifetime lifetime = Lifetime.Transient)
            => self.Lazy(iface, name, new LazyTypeResolver(iface, asmPath, className), lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request. Type resolution is done by the <see cref="LazyTypeResolver"/>.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="iface">The service interface to be registered. It can not be null and can be registered only once.</param>
        /// <param name="asmPath">The absolute path of the containing <see cref="System.Reflection.Assembly"/>.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <paramref name="iface"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy(this IServiceContainer self, Type iface, string asmPath, string className, Lifetime lifetime = Lifetime.Transient)
            => self.Lazy(iface, null, asmPath, className, lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="implementation">The <see cref="ITypeResolver"/> is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <typeparamref name="TInterface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter. For an implementation see the <see cref="LazyTypeResolver{TInterface}"/> class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy<TInterface>(this IServiceContainer self, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient) where TInterface : class
            => self.Lazy<TInterface>(null, implementation, lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The <see cref="ITypeResolver"/> is responsible for resolving the implementation. The resolved <see cref="Type"/> can not be null and must implement the <typeparamref name="TInterface"/> interface. Additionally it must have only null or one constructor (that may request another dependecies). The resolver is called only once (on the first request) regardless the value of the <paramref name="lifetime"/> parameter. For an implementation see the <see cref="LazyTypeResolver{TInterface}"/> class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy<TInterface>(this IServiceContainer self, string? name, ITypeResolver implementation, Lifetime lifetime = Lifetime.Transient) where TInterface : class
            => self.Lazy(typeof(TInterface), name, implementation, lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request. Type resolution is done by the <see cref="LazyTypeResolver"/>.
        /// </summary>
        ///<typeparam name="TInterface">The service interface to be registered. It can be registered only once (with the given <paramref name="name"/>).</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="asmPath">The absolute path of the containing <see cref="System.Reflection.Assembly"/>.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <typeparamref name="TInterface"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy<TInterface>(this IServiceContainer self, string? name, string asmPath, string className, Lifetime lifetime = Lifetime.Transient) where TInterface : class
            => self.Lazy(typeof(TInterface), name, asmPath, className, lifetime);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request. It is useful when the implementation is unknown in compile time or you just want to load the containing assembly on the first request. Type resolution is done by the <see cref="LazyTypeResolver"/>.
        /// </summary>
        /// <typeparam name="TInterface">The service interface to be registered. It can be registered only once.</typeparam>
        /// <param name="self">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="asmPath">The absolute path of the containing <see cref="System.Reflection.Assembly"/>.</param>
        /// <param name="className">The full name of the <see cref="Type"/> that implemenets the <typeparamref name="TInterface"/>.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        /// <returns>The container itself.</returns>
        public static IServiceContainer Lazy<TInterface>(this IServiceContainer self, string asmPath, string className, Lifetime lifetime = Lifetime.Transient) where TInterface : class
            => self.Lazy<TInterface>(null, asmPath, className, lifetime);
    } 
}