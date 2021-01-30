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
        public virtual bool ShouldUse(Injector injector, AbstractServiceEntry requested) => requested.Owner == injector.UnderlyingContainer;

        protected static IServiceReference ExecInternal(Injector injector, IServiceReference? requestor, AbstractServiceEntry requested)
        {
            IServiceReference result = new ServiceReference(requested, injector);

            try
            {
                injector.Instantiate(result);
            }
            catch
            {
                result.Release();
                throw;
            }

            return result;
        }

        [SuppressMessage("Reliability", "CA2000: Dispose objects before losing scope", Justification = "Calling the Release() method disposes the object")]
        IServiceReference IServiceInstantiationStrategy.Exec(Injector injector, IServiceReference? requestor, AbstractServiceEntry requested) 
        {
            if (requested.Built)
                return requested.Instances.Single();

            return ExecInternal(injector, requestor, requested);
        }
    }
}
