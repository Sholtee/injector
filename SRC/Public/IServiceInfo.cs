/********************************************************************************
* IServiceInfo.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI
{
    /// <summary>
    /// Provides several basic information about a registered service.
    /// </summary>
    public interface IServiceInfo
    {
        /// <summary>
        /// The service was registered via <see cref="IInjector.Service"/> call.
        /// </summary>
        bool IsService { get; }

        /// <summary>
        /// The service was registered via <see cref="IInjector.Lazy"/> call.
        /// </summary>
        bool IsLazy { get; }

        /// <summary>
        /// The service was registered via <see cref="IInjector.Factory"/> call.
        /// </summary>
        bool IsFactory { get; }

        /// <summary>
        /// The service was registered via <see cref="IInjector.Instance"/> call.
        /// </summary>
        bool IsInstance { get; }
    }
}
