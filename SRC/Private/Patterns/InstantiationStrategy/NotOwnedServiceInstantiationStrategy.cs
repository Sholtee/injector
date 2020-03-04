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
        public override bool ShouldUse(IInjector injector, AbstractServiceEntry requested) => injector.UnderlyingContainer.IsDescendantOf(requested.Owner);

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The lifetime of newly created injector is maintained by its owner.")]
        public override ServiceReference Exec(IInjectorEx injector, ServiceReference requestor, AbstractServiceEntry requested)
        {
            //
            // - ServiceEntry-t zaroljuk h a lock injectorok kozt is ertelmezve legyen.
            // - Ha Singleton szerviz hivatkozik sajat magara itt akkor sincs dead-lock mivel a hivatkozas
            //   ugyanabban a szalban fog tortenni.
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
                // - Az uj injector elettartama meg fogy egyezni a bejegyzes elettartamaval (mivel "requested.Owner"
                //   gyermeke).
                // - Az eredeti (felhasznalo altal hivott) injector felszabaditasa nem befolyasolja a szerviz 
                //   elettartamat.
                //

                injector = injector.Spawn(requested.Owner);

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
