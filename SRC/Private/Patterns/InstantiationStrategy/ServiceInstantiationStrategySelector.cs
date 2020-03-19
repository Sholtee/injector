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

        public IInjectorEx RelatedInjector { get; }

        public ServiceInstantiationStrategySelector(IInjectorEx relatedInjector) => RelatedInjector = relatedInjector;

        public Func<ServiceReference?, ServiceReference> GetStrategyFor(AbstractServiceEntry requested) 
        {
            //
            // Sorrend szamit.
            //

            foreach (IServiceInstantiationStrategy strategy in Strategies)
                if (strategy.ShouldUse(RelatedInjector, requested))
                    return requestor => strategy.Exec(RelatedInjector, requestor, requested);

            throw new InvalidOperationException(Resources.NO_STRATEGY);
        }
    }
}
