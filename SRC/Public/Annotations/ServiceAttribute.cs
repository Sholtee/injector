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
    public sealed class ServiceAttribute: ServiceRegistrationAttribute
    {
        /// <summary>
        /// The service interface implemented by the marked class.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The <see cref="Lifetime"/> of the service being registered.
        /// </summary>
        public Lifetime Lifetime { get; }

        /// <summary>
        /// Registers a service calling the <see cref="IServiceContainerExtensions.Service(IServiceContainer, Type, Type, Lifetime)"/> method.
        /// </summary>
        /// <param name="interface">The service interface implemented by the marked class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public ServiceAttribute(Type @interface, Lifetime lifetime = Lifetime.Transient)
        {
            Interface = @interface;
            Lifetime = lifetime;
        }

        /// <summary>
        /// See <see cref="ServiceRegistrationAttribute"/>.
        /// </summary>
        public override void Register(IServiceContainer container, Type target)
        {
            container.Service(Interface, target, Lifetime);
        }
    }
}
