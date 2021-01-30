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

    internal sealed class ServiceInstantiationStrategySelector
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

        public Injector RelatedInjector { get; }

        public ServiceInstantiationStrategySelector(Injector relatedInjector) => RelatedInjector = relatedInjector;

        internal Func<IServiceReference?, IServiceReference> GetStrategyInvocation(IServiceInstantiationStrategy strategy, AbstractServiceEntry entry)
        {
            return InvokeStrategy;

            IServiceReference InvokeStrategy(IServiceReference? requestor)
            {
                IServiceReference requested = strategy.Exec(RelatedInjector, requestor, entry);

                //
                // Ha az aktualisan lekerdezett szerviz valakinek a fuggosege akkor hozzaadjuk a fuggosegi listahoz.
                //

                requestor?.AddDependency(requested);
                return requested;
            }
        }

        public Func<IServiceReference?, IServiceReference> GetStrategyFor(AbstractServiceEntry requestedEntry) 
        {
            foreach (IServiceInstantiationStrategy strategy in Strategies)
            {
                if (strategy.ShouldUse(RelatedInjector, requestedEntry))
                    return GetStrategyInvocation(strategy, requestedEntry);
            }

            throw new InvalidOperationException(Resources.NO_STRATEGY);
        }
    }
}
