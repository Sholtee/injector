/********************************************************************************
* OwnedServiceInstantiationStrategy.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal class OwnedServiceInstantiationStrategy: IServiceInstantiationStrategy
    {
        public virtual bool ShouldUse(Injector injector, AbstractServiceEntry requested) => requested.Owner == injector.UnderlyingContainer;

        public virtual ServiceReference Exec(Injector injector, ServiceReference? requestor, AbstractServiceEntry requested) 
        {
            ServiceReference? result = requested.Instance;

            if (result == null)
            {
                result = new ServiceReference(requested, injector);

                try
                {
                    injector.Instantiate(result);
                }
                catch 
                {
                    result.Dispose();
                    throw;
                }
            }

            //
            // Ha az aktualisan lekerdezett szerviz valakinek a fuggosege akkor hozzaadjuk a fuggosegi listahoz.
            //

            requestor?.Dependencies.Add(result);

            return result;
        }
    }
}
