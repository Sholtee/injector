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
        /// Returns true if the service was registered via <see cref="IServiceCollectionBasicExtensions.Service(IServiceCollection, Type, object?, Type, LifetimeBase, ServiceOptions?)"/> call.
        /// </summary>
        public static bool IsService(this AbstractServiceEntry self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return self.Implementation is not null;
        }

        /// <summary>
        /// Returns true if the service was registered via <see cref="IServiceCollectionBasicExtensions.Factory(IServiceCollection, Type, object?, Expression{FactoryDelegate}, LifetimeBase, ServiceOptions?)"/> call.
        /// </summary>
        public static bool IsFactory(this AbstractServiceEntry self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return !self.IsService() && self.Factory is not null;
        }
    }
}