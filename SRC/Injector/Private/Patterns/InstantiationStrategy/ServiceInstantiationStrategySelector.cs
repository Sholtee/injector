/********************************************************************************
* ServiceInstantiationStrategySelector.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal static class ServiceInstantiationStrategySelector
    {
        //
        // Szalbiztosak
        //

        private static readonly IReadOnlyCollection<IServiceInstantiationStrategy> Strategies = new IServiceInstantiationStrategy[]
        {
            //
            // Sorrend szamit.
            //

            new InstanceStrategy(),
            new OwnedServiceInstantiationStrategy(),
            new NotOwnedServiceInstantiationStrategy()
        };

        internal static Func<IServiceReference?, IServiceReference> GetStrategyInvocation(IServiceInstantiationStrategy strategy, Injector injector, AbstractServiceEntry entry)
        {
            return InvokeStrategy;

            IServiceReference InvokeStrategy(IServiceReference? requestor)
            {
                IServiceReference requested = strategy.Exec(injector, entry);

                //
                // Ha az aktualisan lekerdezett szerviz valakinek a fuggosege akkor hozzaadjuk a fuggosegi listahoz.
                //

                requestor?.AddDependency(requested);
                return requested;
            }
        }

        public static Func<IServiceReference?, IServiceReference> GetStrategyFor(Injector injector, AbstractServiceEntry requestedEntry) 
        {
            foreach (IServiceInstantiationStrategy strategy in Strategies)
            {
                if (strategy.ShouldUse(injector, requestedEntry))
                    return GetStrategyInvocation(strategy, injector, requestedEntry);
            }

            throw new InvalidOperationException(Resources.NO_STRATEGY);
        }
    }
}
