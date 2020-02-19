/********************************************************************************
* InstanceStrategy.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal class InstanceStrategy: IServiceInstantiationStrategy
    {
        public bool ShouldUse(Injector injector, AbstractServiceEntry requested) => requested.Instance != null;
        public ServiceReference Exec(Injector injector, ServiceReference requestor, AbstractServiceEntry requested)
        {
            ServiceReference existing = requested.Instance;

            requestor?.Dependencies.Add(existing);
            return existing;
        }
    }
}
