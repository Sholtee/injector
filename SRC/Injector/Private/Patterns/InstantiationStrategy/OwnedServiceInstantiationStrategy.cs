/********************************************************************************
* OwnedServiceInstantiationStrategy.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class OwnedServiceInstantiationStrategy: IServiceInstantiationStrategy
    {
        public bool ShouldUse(Injector injector, AbstractServiceEntry requested) => requested.Owner == injector.UnderlyingContainer;

        [SuppressMessage("Reliability", "CA2000: Dispose objects before losing scope", Justification = "Calling the Release() method disposes the object")]
        IServiceReference IServiceInstantiationStrategy.Exec(Injector injector, AbstractServiceEntry requested) => requested.Built
            ? requested.Instances.Single()
            : injector.Instantiate(requested);
    }
}
