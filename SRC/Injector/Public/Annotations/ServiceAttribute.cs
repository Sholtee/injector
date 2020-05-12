/********************************************************************************
* ServiceAttribute.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Internals;

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
        /// The (optional) name of the service.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The <see cref="Lifetime"/> of the service being registered.
        /// </summary>
        public Lifetime Lifetime { get; }

        /// <summary>
        /// Registers a service by calling the <see cref="IServiceContainerExtensions.Service(IServiceContainer, Type, Type, Lifetime)"/> method.
        /// </summary>
        /// <param name="interface">The service interface implemented by the marked class.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public ServiceAttribute(Type @interface, Lifetime lifetime = Lifetime.Transient)
        {
            Ensure.Parameter.IsNotNull(@interface, nameof(@interface));

            Interface = @interface;
            Lifetime = lifetime;
        }

        /// <summary>
        /// Registers a service by calling the <see cref="IServiceContainerExtensions.Service(IServiceContainer, Type, string, Type, Lifetime)"/> method.
        /// </summary>
        /// <param name="interface">The service interface implemented by the marked class.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public ServiceAttribute(Type @interface, string name, Lifetime lifetime = Lifetime.Transient): this(@interface, lifetime)
        {
            Name = name;
        }

        /// <summary>
        /// See <see cref="ServiceRegistrationAttribute"/>.
        /// </summary>
        public override void Register(IServiceContainer container, Type target)
        {
            Ensure.Parameter.IsNotNull(container, nameof(container));
            Ensure.Parameter.IsNotNull(target, nameof(target));

            container.Service(Interface, Name, target, Lifetime);
        }
    }
}
