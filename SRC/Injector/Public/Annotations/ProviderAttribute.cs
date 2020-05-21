/********************************************************************************
* ServiceAttribute.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    /// <summary>
    /// Indicates that a class is being used as a provider.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ProviderAttribute : ServiceRegistrationAttribute
    {
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
        public Lifetime Lifetime { get; }

        /// <summary>
        /// Registers a provider by calling the <see cref="IServiceContainerExtensions.Provider(IServiceContainer, Type, Type, Lifetime)"/> method.
        /// </summary>
        /// <param name="interface">The service interface.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public ProviderAttribute(Type @interface, Lifetime lifetime = Lifetime.Transient)
        {
            Ensure.Parameter.IsNotNull(@interface, nameof(@interface));

            Interface = @interface;
            Lifetime = lifetime;
        }

        /// <summary>
        /// Registers a provider by calling the <see cref="IServiceContainerExtensions.Provider(IServiceContainer, Type, string?, Type, Lifetime)"/> method.
        /// </summary>
        /// <param name="interface">The service interface.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public ProviderAttribute(Type @interface, string name, Lifetime lifetime = Lifetime.Transient): this(@interface, lifetime)
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

            container.Provider(Interface, Name, target, Lifetime);
        }
    }
}
