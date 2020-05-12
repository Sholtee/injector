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

    /// <summary>
    /// Defines several handy extensions for the <see cref="IInjector"/> interface.
    /// </summary>
    public static class IInjectorExtensions
    {
        /// <summary>
        /// Gets the service instance associated with the given interface and (optional) name.
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service to be resolved. It must be an interface.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        public static TInterface Get<TInterface>(this IInjector self, string? name = null) where TInterface : class
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            return (TInterface) self.Get(typeof(TInterface), name);
        }

        /// <summary>
        /// Tries to get the service instance associated with the given interface and (optional) name.
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service to be resolved. It must be an interface.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The requested service instance if the resolution was successful, null otherwise.</returns>
        public static TInterface? TryGet<TInterface>(this IInjector self, string? name = null) where TInterface : class
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            return (TInterface?) self.TryGet(typeof(TInterface), name);
        }

        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <param name="self">The injector itself.</param>
        /// <param name="class">The class to be instantiated.</param>
        /// <param name="explicitArgs">The explicit arguments (in the form of [parameter name - parameter value]). Explicit arguments won't be resolved by the injector.</param>
        /// <returns>The new instance.</returns>
        /// <remarks>The <paramref name="class"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>. Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as a normal dependency.</remarks>
        /// <exception cref="ServiceNotFoundException">One or more dependecies could not be found.</exception>
        public static object Instantiate(this IInjector self, Type @class, IReadOnlyDictionary<string, object>? explicitArgs = null)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));
            Ensure.Parameter.IsNotNull(@class, nameof(@class));

            return Resolver.GetExtended(@class).Invoke(self, explicitArgs ?? new Dictionary<string, object>(0));
        }
        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <typeparam name="TClass">The class to be instantiated.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="explicitArgs">The explicit arguments (in the form of [parameter name - parameter value]) not to be resolved by the injector.</param>
        /// <returns>The new instance.</returns>
        /// <remarks>The <typeparamref name="TClass"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>. Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as normal dependency.</remarks>
        public static TClass Instantiate<TClass>(this IInjector self, IReadOnlyDictionary<string, object>? explicitArgs = null) where TClass : class
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            return (TClass) self.Instantiate(typeof(TClass), explicitArgs);
        }
    }
}