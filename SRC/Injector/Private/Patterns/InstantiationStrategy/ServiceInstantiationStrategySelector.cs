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

        public Func<IServiceReference?, IServiceReference> GetStrategyFor(AbstractServiceEntry requested) 
        {
            foreach (IServiceInstantiationStrategy strategy in Strategies)
                if (strategy.ShouldUse(RelatedInjector, requested))
                    return requestor => strategy.Exec(RelatedInjector, requestor, requested);

            throw new InvalidOperationException(Resources.NO_STRATEGY);
        }
    }
}
