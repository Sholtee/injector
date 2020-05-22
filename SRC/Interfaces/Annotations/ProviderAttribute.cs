/********************************************************************************
* ProviderAttribute.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Indicates that a class is being used as a service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ProviderAttribute : ServiceRegistrationAttribute
    {
        /// <summary>
        /// Registers a service.
        /// </summary>
        /// <param name="interface">The service interface implemented by the marked class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public ProviderAttribute(Type @interface, Lifetime lifetime = Interfaces.Lifetime.Transient) : base(@interface, null, lifetime)
        {
        }

        /// <summary>
        /// Registers a service.
        /// </summary>
        /// <param name="interface">The service interface implemented by the marked class.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public ProviderAttribute(Type @interface, string name, Lifetime lifetime = Interfaces.Lifetime.Transient) : base(@interface, name, lifetime)
        {
        }

        internal override string Implementation => "Solti.Utils.DI.Internals.ProviderRegistration";
    }
}
