/********************************************************************************
* OwnedServiceInstantiationStrategy.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class OwnedServiceInstantiationStrategy: IServiceInstantiationStrategy
    {
        public virtual bool ShouldUse(Injector injector, AbstractServiceEntry requested) => requested.Owner == injector.UnderlyingContainer;

        [SuppressMessage("Reliability", "CA2000: Dispose objects before losing scope", Justification = "Calling the Release() method disposes the object")]
        public virtual IServiceReference Exec(Injector injector, IServiceReference? requestor, AbstractServiceEntry requested) 
        {
            IServiceReference? result = requested.Instance;

            if (result == null)
            {
                result = new ServiceReference(requested, injector);

                try
                {
                    injector.Instantiate(result);
                }
                catch 
                {
                    result.Release();
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
