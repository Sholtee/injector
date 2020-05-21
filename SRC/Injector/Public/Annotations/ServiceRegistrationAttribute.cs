/********************************************************************************
* ServiceRegistrationAttribute.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;

    /// <summary>
    /// Base class of "auto config" attributes.
    /// </summary>
    public abstract class ServiceRegistrationAttribute: Attribute
    {
        /// <summary>
        /// The registration logic.
        /// </summary>
        /// <param name="container">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="target">The target <see cref="Type"/> on which the attribute was applied.</param>
        public abstract void Register(IServiceContainer container, Type target);
    }
}
