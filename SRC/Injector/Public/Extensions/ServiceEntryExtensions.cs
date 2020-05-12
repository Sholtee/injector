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
        /// The service was registered via <see cref="IServiceContainerExtensions.Service(IServiceContainer, Type, string, Type, Lifetime)"/> call.
        /// </summary>
        public static bool IsService(this AbstractServiceEntry self)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            return self.Implementation != null;
        }

        /// <summary>
        /// The service was registered via <see cref="IServiceContainerExtensions.Factory(IServiceContainer, Type, string, Func{IInjector, Type, object}, Lifetime)"/> call.
        /// </summary>
        public static bool IsFactory(this AbstractServiceEntry self)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            return !self.IsService() && self.Factory != null;
        }

        /// <summary>
        /// The service was registered via <see cref="IServiceContainerExtensions.Instance(IServiceContainer, Type, string, object, bool)"/> call.
        /// </summary>
        public static bool IsInstance(this AbstractServiceEntry self)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            return self.Lifetime == null && self.Instance?.Value != null;
        }
    }
}