/********************************************************************************
* IInjectorExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Internals;
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
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        public static TInterface Get<TInterface>(this IInjector self, string name = null) => self != null ? (TInterface) self.Get(typeof(TInterface), name) : throw new ArgumentNullException(nameof(self));

        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <param name="self">The injector itself.</param>
        /// <param name="class">The class to be instantiated.</param>
        /// <param name="explicitArgs">The explicit arguments (in the form of [parameter name - parameter value]). Explicit arguments won't be resolved by the injector.</param>
        /// <returns>The new instance.</returns>
        /// <remarks>The <paramref name="class"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>. Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as a normal dependency.</remarks>
        /// <exception cref="ServiceNotFoundException">One or more dependecies could not be found.</exception>
        public static object Instantiate(this IInjector self, Type @class, IReadOnlyDictionary<string, object> explicitArgs = null)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));

            if (@class == null)
                throw new ArgumentNullException(nameof(@class));

            return Resolver.GetExtended(@class)(self, explicitArgs ?? new Dictionary<string, object>(0));
        }
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
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="self">The injector itself.</param>
        /// <returns>The <see cref="Lifetime"/> of the service if it is producible, null otherwise.</returns>
        public static Lifetime? LifetimeOf<TInterface>(this IInjector self, string name = null) => self != null ? self.LifetimeOf(typeof(TInterface), name) : throw new ArgumentNullException(nameof(self));
    }
}