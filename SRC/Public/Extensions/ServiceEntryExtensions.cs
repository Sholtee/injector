/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Internals;

    /// <summary>
    /// Defines several handy extensions for the <see cref="AbstractServiceEntry"/> class.
    /// </summary>
    public static class ServiceEntryExtensions
    {
        /// <summary>
        /// The service was registered via <see cref="IServiceContainerExtensions.Service"/> call.
        /// </summary>
        public static bool IsService(this AbstractServiceEntry self) => self.UserData != null;

        /// <summary>
        /// The service was registered via <see cref="IServiceContainerExtensions.Lazy"/> call.
        /// </summary>
        public static bool IsLazy(this AbstractServiceEntry self) => self.UserData is Lazy<Type>;

        /// <summary>
        /// The service was registered via <see cref="IServiceContainerExtensions.Factory"/> call.
        /// </summary>
        public static bool IsFactory(this AbstractServiceEntry self) => !self.IsService() && self.Factory != null;

        /// <summary>
        /// The service was registered via <see cref="IServiceContainerExtensions.Instance"/> call.
        /// </summary>
        public static bool IsInstance(this AbstractServiceEntry self) => !self.IsService() && !self.IsFactory() && self.Value != null;
    }
}