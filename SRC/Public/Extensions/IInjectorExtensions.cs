/********************************************************************************
* IInjectorExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.DI
{
    using Annotations;

    /// <summary>
    /// Defines several handy extensions for the <see cref="IInjector"/> interface.
    /// </summary>
    public static class IInjectorExtensions
    {
        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service to be resolved. It must be an interface.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="target">The (optional) target who requested the dependency.</param>
        /// <returns>The resolved service.</returns>
        /// <remarks>This method is thread safe so you can call it parallelly.</remarks>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        public static TInterface Get<TInterface>(this IInjector self, Type target = null) => self != null ? (TInterface) self.Get(typeof(TInterface), target) : throw new ArgumentNullException(nameof(self));

        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <typeparam name="TClass">The class to be instantiated.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="explicitArgs">The explicit arguments (in the form of [parameter name - parameter value]) not to be resolved by the injector.</param>
        /// <returns>The new instance.</returns>
        /// <remarks>The <typeparamref name="TClass"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>. Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as normal dependency.</remarks>
        public static TClass Instantiate<TClass>(this IInjector self, IReadOnlyDictionary<string, object> explicitArgs = null) => self != null ? (TClass) self.Instantiate(typeof(TClass), explicitArgs) : throw new ArgumentNullException(nameof(self));

        /// <summary>
        /// Gets the <see cref="Lifetime"/> of the given service (type).
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <returns>The <see cref="Lifetime"/> of the service if it is producible null otherwise.</returns>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        public static Lifetime? LifetimeOf<TInterface>(this IInjector self) => self != null ? self.LifetimeOf(typeof(TInterface)) : throw new ArgumentNullException(nameof(self));

        /// <summary>
        /// Returns true if you should release service(s) with the given interface, false otherwise.
        /// </summary>
        /// <param name="self">The injector itself.</param>
        /// <param name="iface">The interface to be checked.</param>
        /// <remarks>This method is intended to be called in the Dispose method of an object.</remarks>
        public static bool ShouldRelease(this IInjector self, Type iface) => typeof(IDisposable).IsAssignableFrom(iface) && self.LifetimeOf(iface) == Lifetime.Transient;

        /// <summary>
        /// Returns true if you should release service(s) with the given <typeparamref name="TInterface"/>, false otherwise.
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <param name="self">The injector itself.</param>
        /// <remarks>This method is intended to be called in the Dispose method of an object.</remarks>
        public static bool ShouldRelease<TInterface>(this IInjector self) => self.ShouldRelease(typeof(TInterface));
    }
}