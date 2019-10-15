/********************************************************************************
* ServiceInterfaceOfAttribute.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Annotations
{
    /// <summary>
    /// Indicates that the interface is being used as a service interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class ServiceInterfaceOfAttribute : ServiceRegistrationAttribute
    {
        private readonly Action<IServiceContainer, Type> RegisterFn;

        /// <summary>
        /// Registers a service calling the <see cref="IServiceContainerExtensions.Service(IServiceContainer, Type, Type, Lifetime)"/> method.
        /// </summary>
        /// <param name="implementation">The service implementation.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public ServiceInterfaceOfAttribute(Type implementation, Lifetime lifetime = Lifetime.Transient) => 
            RegisterFn = (container, iface) => container.Service(iface, implementation, lifetime);

        /// <summary>
        /// Registers a service calling the <see cref="IServiceContainerExtensions.Lazy(IServiceContainer, Type, ITypeResolver, Lifetime)"/> method.
        /// </summary>
        /// <param name="asmPath">The absolute path of the <see cref="System.Reflection.Assembly"/> containing the implementation.</param>
        /// <param name="className">The full name of the implementation.</param>
        /// <param name="lifetime">The <see cref="Lifetime"/> of the service.</param>
        public ServiceInterfaceOfAttribute(string asmPath, string className, Lifetime lifetime = Lifetime.Transient) => 
            RegisterFn = (container, iface) => container.Lazy(iface, new LazyTypeResolver(iface, asmPath, className), lifetime);

        /// <summary>
        /// See <see cref="ServiceRegistrationAttribute"/>.
        /// </summary>
        public override void Register(IServiceContainer container, Type target) => RegisterFn(container, target);
    }
}
