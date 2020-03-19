﻿/********************************************************************************
* InstanceStrategy.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal class InstanceStrategy: IServiceInstantiationStrategy
    {
        public bool ShouldUse(IInjector injector, AbstractServiceEntry requested) => requested.Instance != null;
       
        public ServiceReference Exec(IInjectorEx injector, ServiceReference? requestor, AbstractServiceEntry requested)
        {
            //
            // Ide csak akkor juthatunk el ha "requested.Instance" nem NULL [lasd ShouldUse()]
            //

            ServiceReference existing = Ensure.IsNotNull(requested.Instance, $"{nameof(requested)}.{nameof(requested.Instance)}");

            requestor?.Dependencies.Add(existing);
            return existing;
        }
    }
}
