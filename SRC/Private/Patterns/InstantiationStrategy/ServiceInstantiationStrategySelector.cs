/********************************************************************************
* ServiceInstantiationStrategySelector.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class ServiceInstantiationStrategySelector
    {
        //
        // Szalbiztosak
        //

        private static readonly IServiceInstantiationStrategy
            InstanceStrategy = new InstanceStrategy(),
            OwnedServiceInstantiationStrategy = new OwnedServiceInstantiationStrategy(),
            NotOwnedServiceInstantiationStrategy = new NotOwnedServiceInstantiationStrategy();

        public IStatefulInjector RelatedInjector { get; }

        public ServiceInstantiationStrategySelector(IStatefulInjector relatedInjector) => RelatedInjector = relatedInjector;

        public Func<ServiceReference> GetStrategyFor(AbstractServiceEntry requested) 
        {
            //
            // Sorrend szamit.
            //

            foreach (IServiceInstantiationStrategy strategy in new[] { InstanceStrategy, OwnedServiceInstantiationStrategy, NotOwnedServiceInstantiationStrategy })
                if (strategy.ShouldUse(RelatedInjector, requested))
                    return () => strategy.Exec(RelatedInjector, RelatedInjector.Graph.Current, requested);

            throw new InvalidOperationException(Resources.NO_STRATEGY);
        }
    }
}
