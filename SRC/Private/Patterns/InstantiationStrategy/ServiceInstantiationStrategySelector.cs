/********************************************************************************
* ServiceInstantiationStrategySelector.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class ServiceInstantiationStrategySelector
    {
        //
        // Szalbiztosak
        //

        private static readonly IReadOnlyCollection<IServiceInstantiationStrategy> Strategies = new IServiceInstantiationStrategy[]
        {
            new InstanceStrategy(),
            new OwnedServiceInstantiationStrategy(),
            new NotOwnedServiceInstantiationStrategy()
        };

        public IStatefulInjector RelatedInjector { get; }

        public ServiceInstantiationStrategySelector(IStatefulInjector relatedInjector) => RelatedInjector = relatedInjector;

        public Func<ServiceReference> GetStrategyFor(AbstractServiceEntry requested) 
        {
            //
            // Sorrend szamit.
            //

            foreach (IServiceInstantiationStrategy strategy in Strategies)
                if (strategy.ShouldUse(RelatedInjector, requested))
                    return () => strategy.Exec(RelatedInjector, RelatedInjector.Graph.Current, requested);

            throw new InvalidOperationException(Resources.NO_STRATEGY);
        }
    }
}
