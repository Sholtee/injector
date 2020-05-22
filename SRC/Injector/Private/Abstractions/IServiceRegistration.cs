/********************************************************************************
* IServiceRegistration.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    
    internal interface IServiceRegistration
    {
        void Invoke(IServiceContainer container, ServiceRegistrationAttribute registration, Type referencedType);
    }
}
