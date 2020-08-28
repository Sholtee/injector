/********************************************************************************
* ServiceRegistration.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// Used by <see cref="ServiceAttribute.Implementation"/>
    /// </summary>
    internal sealed class ServiceRegistration : IServiceRegistration
    {
        public void Invoke(IServiceContainer container, ServiceRegistrationAttribute registration, Type referencedType) => container.Service(
            registration.Interface, 
            registration.Name, 
            referencedType, 
            registration.Lifetime!.Value);
    }
}
