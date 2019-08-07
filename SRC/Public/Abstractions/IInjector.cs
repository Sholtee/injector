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
    /// <remarks>Since requesting a generic service may cause write operations the <see cref="IInjector"/> should be treated as not thread safe.</remarks>
    public interface IInjector: IQueryServiceInfo, IDisposable
    {
        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <param name="iface">The "id" of the service to be resolved. It must be a non-generic interface.</param>
        /// <param name="target">The (optional) target who requested the dependency.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="NotSupportedException">The service can not be found.</exception>
        object Get([ParameterIs(typeof(NotNull), typeof(Interface), typeof(NotGeneric))] Type iface, [ParameterIs(typeof(Class))] Type target = null);

        /// <summary>
        /// Instantiates the given class.
        /// </summary>
        /// <param name="class">The class to be instantiated.</param>
        /// <param name="explicitArgs">The explicit arguments (in the form of [parameter name - parameter value]) not to be resolved by the injector.</param>
        /// <returns>The new instance.</returns>
        /// <remarks>The <paramref name="@class"/> you passed must have only one public constructor or you must annotate the appropriate one with the <see cref="ServiceActivatorAttribute"/>. Constructor parameteres that are not present in the <paramref name="explicitArgs"/> are treated as a normal dependency.</remarks>
        object Instantiate([ParameterIs(typeof(NotNull), typeof(NotGeneric), typeof(Class))] Type @class, IReadOnlyDictionary<string, object> explicitArgs = null);

        /// <summary>
        /// The event fired before a service requested. It's useful when you want to resolve contextual dependencies or return service mocks.
        /// </summary>
        event InjectorEventHandler<InjectorEventArg> OnServiceRequest;

        /// <summary>
        /// The event fired after a service requested.
        /// </summary>
        /// <remarks>The handler might specifiy a mock service to be returned.</remarks>
        event InjectorEventHandler<InjectorEventArg> OnServiceRequested;
    }
}