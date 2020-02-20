/********************************************************************************
* InstanceStrategy.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal class InstanceStrategy: IServiceInstantiationStrategy
    {
        public bool ShouldUse(IInjector injector, AbstractServiceEntry requested) => requested.Instance != null;
        public ServiceReference Exec(IStatefulInjector injector, ServiceReference requestor, AbstractServiceEntry requested)
        {
            ServiceReference existing = requested.Instance;

            requestor?.Dependencies.Add(existing);
            return existing;
        }
    }
}
