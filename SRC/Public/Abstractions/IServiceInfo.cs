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
        /// Factory function (if exists).
        /// </summary>
        /// <remarks>Proxying a service can change this function.</remarks>
        Func<IInjector, Type, object> Factory { get; }

        /// <summary>
        /// A value associated with this entry. It is not-null only if the <see cref="IServiceInfo.IsInstance"/> is true or the <see cref="IServiceInfo.Lifetime"/> is <see cref="Lifetime.Singleton"/>.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// The service was registered via <see cref="IServiceContainer.Service"/> call.
        /// </summary>
        bool IsService { get; }

        /// <summary>
        /// The service was registered via <see cref="IServiceContainer.Lazy"/> call.
        /// </summary>
        bool IsLazy { get; }

        /// <summary>
        /// The service was registered via <see cref="IServiceContainer.Factory"/> call.
        /// </summary>
        bool IsFactory { get; }

        /// <summary>
        /// The service was registered via <see cref="IServiceContainer.Instance"/> call.
        /// </summary>
        bool IsInstance { get; }

        /// <summary>
        /// The lifetime of the service.
        /// </summary>
        /// <remarks>Instances have no lifetime (unless the releaseOnDispose parameter was set to true).</remarks>
        Lifetime? Lifetime { get; }
    }
}
