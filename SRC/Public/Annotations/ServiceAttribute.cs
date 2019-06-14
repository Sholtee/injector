/********************************************************************************
* ServiceAttribute.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Annotations
{
    /// <summary>
    /// Indicates that a class is being used as a service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ServiceAttribute: Attribute
    {
        /// <summary>
        /// The service interface implemented by the marked class.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The lifetime of the service.
        /// </summary>
        public Lifetime Lifetime { get; }

        public ServiceAttribute(Type @interface, Lifetime lifetime = Lifetime.Transient)
        {
            Interface = @interface;
            Lifetime  = lifetime;
        }
    }
}
