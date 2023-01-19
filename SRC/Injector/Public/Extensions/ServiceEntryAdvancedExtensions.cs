/********************************************************************************
* ServiceEntryAdvancedExtensions.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;

    /// <summary>
    /// Defines some extensions for the <see cref="AbstractServiceEntry"/> class.
    /// </summary>
    public static class ServiceEntryAdvancedExtensions
    {
        /// <summary>
        /// The service was registered via <see cref="IServiceCollectionAdvancedExtensions.Instance(IServiceCollection, Type, string, object, ServiceOptions?)"/> call.
        /// </summary>
        public static bool IsInstance(this AbstractServiceEntry self)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return self.Lifetime == Lifetime.Instance;
        }
    }
}