/********************************************************************************
* IInjectorAdvancedExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    /// <summary>
    /// Defines advanced extensions for the <see cref="IInjector"/> interface.
    /// </summary>
    public static class IInjectorAdvancedExtensions
    {
        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <param name="self">The injector itself.</param>
        /// <param name="class">The class to be instantiated.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user (may be an anonym object or a <see cref="IReadOnlyDictionary{TKey, TValue}"/> where the key is <see cref="string"/> and value is <see cref="object"/>).</param>
        /// <returns>The new instance.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>The <paramref name="class"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>.</description></item>
        /// <item><description>Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as a normal dependency.</description></item>
        /// <item><description>The caller is responsible for freeing the returned instance.</description></item>
        /// </list>
        /// </remarks>        
        /// <exception cref="ServiceNotFoundException">One or more dependecies could not be found.</exception>
        public static object Instantiate(this IInjector self, Type @class, object explicitArgs)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));
            Ensure.Parameter.IsNotNull(@class, nameof(@class));
            Ensure.Parameter.IsNotNull(explicitArgs, nameof(explicitArgs));

            return explicitArgs is IReadOnlyDictionary<string, object?> dict
                ? ServiceActivator.GetExtended(@class).Invoke(self, dict)
                : ServiceActivator.GetExtended(@class, explicitArgs.GetType()).Invoke(self, explicitArgs);
        }

        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <typeparam name="TClass">The class to be instantiated.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="explicitArgs">Explicit arguments, provided by the user (may be an anonym object or a <see cref="IReadOnlyDictionary{TKey, TValue}"/> where the key is <see cref="string"/> and value is <see cref="object"/>).</param>
        /// <returns>The new instance.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>The <typeparamref name="TClass"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>.</description></item>
        /// <item><description>Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as a normal dependency.</description></item>
        /// <item><description>The caller is responsible for freeing the returned instance.</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ServiceNotFoundException">One or more dependecies could not be found.</exception>
        public static TClass Instantiate<TClass>(this IInjector self, object explicitArgs) where TClass : class => (TClass) self.Instantiate(typeof(TClass), explicitArgs);
    }
}