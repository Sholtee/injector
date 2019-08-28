/********************************************************************************
* IServiceInfo.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Provides several basic information about a registered service.
    /// </summary>
    public interface IServiceInfo
    {
        /// <summary>
        /// The service interface.
        /// </summary>
        Type Interface { get; }

        /// <summary>
        /// The service implementation (can be open an generic type).
        /// </summary>
        /// <remarks>If the entry belongs to a lazy service, getting this property will trigger the type resolver.</remarks>
        Type Implementation { get; }

        /// <summary>
        /// Gets the underlying implementation.
        /// </summary>
        object UnderlyingImplementation { get; }

        /// <summary>
        /// Factory function (if exists).
        /// </summary>
        /// <remarks>Proxying a service can change this function.</remarks>
        Func<IInjector, Type, object> Factory { get; }

        /// <summary>
        /// A value associated with this entry (if exists).
        /// </summary>
        object Value { get; }

        /// <summary>
        /// The lifetime of the service.
        /// </summary>
        /// <remarks>Instances have no lifetime.</remarks>
        Lifetime? Lifetime { get; }
    }
}
