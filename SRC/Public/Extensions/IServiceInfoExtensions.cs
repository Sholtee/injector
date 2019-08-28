/********************************************************************************
* IServiceInfoExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Defines several handy extensions for the <see cref="IServiceInfo"/> interface.
    /// </summary>
    public static class IServiceInfoExtensions
    {
        /// <summary>
        /// The service was registered via <see cref="IServiceContainer.Service"/> call.
        /// </summary>
        public static bool IsService(this IServiceInfo self) => self.UnderlyingImplementation != null;

        /// <summary>
        /// The service was registered via <see cref="IServiceContainer.Lazy"/> call.
        /// </summary>
        public static bool IsLazy(this IServiceInfo self) => self.UnderlyingImplementation is Lazy<Type>;

        /// <summary>
        /// The service was registered via <see cref="IServiceContainer.Factory"/> call.
        /// </summary>
        public static bool IsFactory(this IServiceInfo self) => !self.IsService() && self.Factory != null;

        /// <summary>
        /// The service was registered via <see cref="IServiceContainer.Instance"/> call.
        /// </summary>
        public static bool IsInstance(this IServiceInfo self) => !self.IsService() && !self.IsFactory() && self.Value != null;
    }
}