/********************************************************************************
* InstanceStrategy.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class InstanceStrategy: IServiceInstantiationStrategy
    {
        public bool ShouldUse(Injector injector, AbstractServiceEntry requested) => requested.Built;

        public IServiceReference Exec(Injector injector, AbstractServiceEntry requested) =>
            requested.Instances.Single();
    }
}
