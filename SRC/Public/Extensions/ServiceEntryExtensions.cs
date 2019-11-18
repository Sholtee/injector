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
        /// The service was registered via <see cref="IServiceContainerExtensions.Service(IServiceContainer, Type, string, Type, Lifetime)"/> or <see cref="IServiceContainerExtensions.Lazy(IServiceContainer, Type, string, ITypeResolver, Lifetime)"/> call.
        /// </summary>
        public static bool IsService(this AbstractServiceEntry self) => self != null 
            ? self.IsLazy() /*nem triggereli a resolvert*/ || self.Implementation != null
            : throw new ArgumentNullException(nameof(self));

        /// <summary>
        /// The service was registered via <see cref="IServiceContainerExtensions.Lazy(IServiceContainer, Type, string, ITypeResolver, Lifetime)"/> call.
        /// </summary>
        public static bool IsLazy(this AbstractServiceEntry self) => self != null
            ? (self as ProducibleServiceEntry)?.UnderlyingImplementation as Lazy<Type> != null
            : throw new ArgumentNullException(nameof(self));

        /// <summary>
        /// The service was registered via <see cref="IServiceContainerExtensions.Factory(IServiceContainer, Type, string, Func{IInjector, Type, object}, Lifetime)"/> call.
        /// </summary>
        public static bool IsFactory(this AbstractServiceEntry self) => self != null 
            ? !self.IsService() && self.Factory != null
            : throw new ArgumentNullException(nameof(self));

        /// <summary>
        /// The service was registered via <see cref="IServiceContainerExtensions.Instance(IServiceContainer, Type, string, object, bool)"/> call.
        /// </summary>
        public static bool IsInstance(this AbstractServiceEntry self) => self != null 
            ? self is InstanceServiceEntry
            : throw new ArgumentNullException(nameof(self));
    }
}