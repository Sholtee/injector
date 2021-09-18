/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;

    /// <summary>
    /// Defines some extensions for the <see cref="AbstractServiceEntry"/> class.
    /// </summary>
    public static class ServiceEntryExtensions
    {
        /// <summary>
        /// The service was registered via <see cref="IServiceCollectionBasicExtensions.Service(IServiceCollection, Type, string, Type, Lifetime)"/> call.
        /// </summary>
        public static bool IsService(this AbstractServiceEntry self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return self.Implementation is not  null;
        }

        /// <summary>
        /// The service was registered via <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type, string, Func{IInjector, Type, object}, Lifetime)"/> call.
        /// </summary>
        public static bool IsFactory(this AbstractServiceEntry self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return !self.IsService() && self.Factory is not null;
        }

        /// <summary>
        /// The service was registered via <see cref="IServiceCollectionBasicExtensions.Instance(IServiceCollection, Type, string, object)"/> call.
        /// </summary>
        public static bool IsInstance(this AbstractServiceEntry self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return self.Lifetime == Lifetime.Instance;
        }

        /// <summary>
        /// Returns true if the entry is intended to internal use.
        /// </summary>
        public static bool IsInternal(this AbstractServiceEntry self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return self.Name?.StartsWith(Consts.INTERNAL_SERVICE_NAME_PREFIX, StringComparison.Ordinal) is true;
        }

        /// <summary>
        /// Applies the given <paramref name="decorator"/>.
        /// </summary>
        public static void ApplyProxy(this AbstractServiceEntry self, Func<IInjector, Type, object, object> decorator)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (decorator is null)
                throw new ArgumentNullException(nameof(decorator));

            if (self is not ISupportsProxying setter || setter.Factory is null)
                throw new InvalidOperationException(Resources.PROXYING_NOT_SUPPORTED);

            //
            // Bovitjuk a hivasi lancot a decorator-al.
            //

            Func<IInjector, Type, object> oldFactory = setter.Factory;

            setter.Factory = (injector, type) => decorator(injector, type, oldFactory(injector, type));
        }
    }
}