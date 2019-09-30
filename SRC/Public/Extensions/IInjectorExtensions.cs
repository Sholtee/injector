/********************************************************************************
* IInjectorExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

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
        public static TInterface Get<TInterface>(this IInjector self, Type target = null) => (TInterface) self.Get(typeof(TInterface), target);

        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <typeparam name="TClass">The class to be instantiated.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="explicitArgs">The explicit arguments (in the form of [parameter name - parameter value]) not to be resolved by the injector.</param>
        /// <returns>The new instance.</returns>
        /// <remarks>The <typeparamref name="TClass"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>. Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as normal dependency.</remarks>
        public static TClass Instantiate<TClass>(this IInjector self, IReadOnlyDictionary<string, object> explicitArgs = null) => (TClass) self.Instantiate(typeof(TClass), explicitArgs);
    }
}