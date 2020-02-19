/********************************************************************************
* NotOwnedServiceInstantiationStrategy.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Internals
{
    internal class NotOwnedServiceInstantiationStrategy: OwnedServiceInstantiationStrategy
    {
        public override bool ShouldUse(Injector injector, AbstractServiceEntry requested) => injector.IsDescendantOf(requested.Owner);

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The lifetime of newly created injector is maintained by its owner.")]
        public override ServiceReference Exec(Injector injector, ServiceReference requestor, AbstractServiceEntry requested)
        {
            //
            // ServiceEntry-t zaroljuk h a lock injectorok kozt is ertelmezve legyen.
            //

            lock (requested)
            {
                //
                // Valaki kozben beallitotta?
                //

                ServiceReference existing = requested.Instance;

                if (existing != null)
                {
                    //
                    // Mivel az os nem kerul meghivasra ezert nekunk kell bovitenunk a fuggosegi listat.
                    //

                    requestor?.Dependencies.Add(existing);
                    return existing;
                }

                //
                // - Az uj injector elettartama meg fogy egyezni a bejegyzes elettartamaval (mivel "entry.Owner"
                //   gyermeke).
                // - Az eredeti (felhasznalo altal hivott) injector felszabaditasa nem befolyasolja a szerviz 
                //   elettartamat.
                //

                injector = new Injector(requested.Owner, injector);

                try
                {
                    return base.Exec(injector, requestor, requested);
                }
                catch
                {
                    injector.Dispose();
                    throw;
                }
            }
        }
    }
}
