/********************************************************************************
* NotOwnedServiceInstantiationStrategy.cs                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class NotOwnedServiceInstantiationStrategy: IServiceInstantiationStrategy
    {
        public bool ShouldUse(Injector injector, AbstractServiceEntry requested) => injector.UnderlyingContainer.IsDescendantOf(requested.Owner);

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The lifetime of newly created injector is maintained by its owner.")]
        IServiceReference IServiceInstantiationStrategy.Exec(Injector injector, AbstractServiceEntry requested)
        {
            //
            // - ServiceEntry-t zaroljuk h a lock injectorok kozt is ertelmezve legyen.
            // - Ha Singleton szerviz hivatkozik sajat magara itt akkor sincs dead-lock mivel a hivatkozas
            //   ugyanabban a szalban fog tortenni.
            // 

            lock (requested)
            {
                //
                // Valaki mar beallitotta?
                //

                if (requested.Built)
                    return requested.Instances.Single();

                //
                // - Az uj injector elettartama meg fogy egyezni a bejegyzes elettartamaval (mivel "requested.Owner"
                //   gyermeke).
                // - Az eredeti (felhasznalo altal hivott) injector felszabaditasa nem befolyasolja a szerviz 
                //   elettartamat.
                //

                injector = injector.Fork(requested.Owner);

                try
                {
                    return injector.Instantiate(requested);
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
