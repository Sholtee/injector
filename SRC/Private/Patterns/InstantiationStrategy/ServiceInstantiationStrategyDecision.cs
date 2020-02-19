/********************************************************************************
* ServiceInstantiationStrategyDecision.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal static class ServiceInstantiationStrategyDecision
    {
        //
        // Szalbiztosak
        //

        private static readonly IServiceInstantiationStrategy
            InstanceStrategy = new InstanceStrategy(),
            OwnedServiceInstantiationStrategy = new OwnedServiceInstantiationStrategy(),
            NotOwnedServiceInstantiationStrategy = new NotOwnedServiceInstantiationStrategy();

        public static Func<ServiceReference, ServiceReference> GetInstantiationStrategy(this Injector injector, AbstractServiceEntry requested) 
        {
            //
            // Sorrend szamit.
            //

            foreach (IServiceInstantiationStrategy strategy in new[] { InstanceStrategy, OwnedServiceInstantiationStrategy, NotOwnedServiceInstantiationStrategy })
                if (strategy.ShouldUse(injector, requested))
                    return requestor => strategy.Exec(injector, requestor, requested);

            throw new InvalidOperationException(Resources.NO_STRATEGY);
        }
    }
}
