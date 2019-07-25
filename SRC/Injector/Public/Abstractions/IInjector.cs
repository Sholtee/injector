/********************************************************************************
* IInjector.cs                                                                  *
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
    /// Provides a mechanism for injecting resources.
    /// </summary>
    public interface IInjector: IQueryServiceInfo, IDisposable
    {
        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be a non-generic interface.</param>
        /// <returns>The resolved service.</returns>
        /// <remarks>This method is thread safe so you can call it parallelly.</remarks>
        /// <exception cref="NotSupportedException">The service can not be found.</exception>
        object Get([ParameterIs(typeof(NotNull), typeof(Interface), typeof(NotGeneric))] Type iface);

        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <param name="class">The class to be instantiated.</param>
        /// <param name="explicitArgs">The explicit arguments (in the form of [parameter name - parameter value]) not to be resolved by the injector.</param>
        /// <returns>The new instance.</returns>
        /// <remarks>The <paramref name="@class"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>. Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as a normal dependency.</remarks>
        object Instantiate([ParameterIs(typeof(NotNull), typeof(NotGeneric), typeof(Class))] Type @class, IReadOnlyDictionary<string, object> explicitArgs = null);
    }
}