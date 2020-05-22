/********************************************************************************
* ServiceRegistrationAttribute.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Base class of "auto config" attributes.
    /// </summary>
    public abstract class ServiceRegistrationAttribute: Attribute
    {
        /// <summary>
        /// Creates a new <see cref="ServiceRegistrationAttribute"/> instance.
        /// </summary>
        protected ServiceRegistrationAttribute(Type @interface, string? name, Lifetime? lifetime): base()
        {
            Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
            Name = name;
            Lifetime = lifetime;
        }

        /// <summary>
        /// The service interface implemented.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The (optional) name of the service.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The <see cref="Lifetime"/> of the service being registered.
        /// </summary>
        public Lifetime? Lifetime { get; }

        internal abstract string Implementation { get; }
    }
}
