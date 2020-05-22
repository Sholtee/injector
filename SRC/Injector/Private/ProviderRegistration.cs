/********************************************************************************
* ProviderRegistration.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ProviderRegistration : IServiceRegistration
    {
        public void Invoke(IServiceContainer container, ServiceRegistrationAttribute registration, Type referencedType) => container.Provider(
            registration.Interface, 
            registration.Name, 
            referencedType, 
            registration.Lifetime!.Value);
    }
}
