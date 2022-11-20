/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines some extensions for the <see cref="AbstractServiceEntry"/> class.
    /// </summary>
    public static class ServiceEntryExtensions
    {
        /// <summary>
        /// The service was registered via <see cref="IServiceCollectionBasicExtensions.Service(IServiceCollection, Type, string, Type, LifetimeBase)"/> call.
        /// </summary>
        public static bool IsService(this AbstractServiceEntry self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return self.Implementation is not null;
        }
        /// <summary>
        /// The service was registered via <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type, string, Expression{FactoryDelegate}, LifetimeBase)"/> call.
        /// </summary>
        public static bool IsFactory(this AbstractServiceEntry self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return !self.IsService() && self.Factory is not null;
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
    }
}