/********************************************************************************
* InstanceStrategy.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class InstanceStrategy: IServiceInstantiationStrategy
    {
        public bool ShouldUse(Injector injector, AbstractServiceEntry requested) => requested.Instance != null;
       
        public IServiceReference Exec(Injector injector, IServiceReference? requestor, AbstractServiceEntry requested)
        {
            //
            // Ide csak akkor juthatunk el ha "requested.Instance" nem NULL [lasd ShouldUse()]
            //

            IServiceReference existing = Ensure.IsNotNull(requested.Instance, $"{nameof(requested)}.{nameof(requested.Instance)}");

            requestor?.Dependencies.Add(existing);
            return existing;
        }
    }
}
