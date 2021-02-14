/********************************************************************************
* Injector.Instantiation.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal partial class Injector
    {
        private void CheckBreaksTheRuleOfStrictDI(AbstractServiceEntry requested)
        {
            if (!Config.Value.Injector.StrictDI) return;

            AbstractServiceEntry? requestor = FGraph.Requestor?.RelatedServiceEntry; // lehet NULL

            //
            // Ha a fuggosegi fa gyokerenel vagyunk akkor a metodus nem ertelmezett.
            //

            if (requestor == null) return;

            //
            // A kerelmezett szerviz tulajdonosanak egy szinten v feljebb kell lennie mint a kerelmezo 
            // tulajdonosa h biztosan legalabb annyi ideig letezzen mint a kerelmezo maga.
            //

            if (!requestor.Owner.IsDescendantOf(requested.Owner))
            {
                var ex = new RequestNotAllowedException(Resources.STRICT_DI);
                ex.Data[nameof(requestor)] = requestor;
                ex.Data[nameof(requested)] = requested;

                throw ex;
            }
        }

        private IServiceReference Instantiate(AbstractServiceEntry requested)
        {
            //
            // Kell egyaltalan peldanyositani?
            //

            if (requested.Built)
                return requested.Instances.Single();

            //
            // Ha nem mi vagyunk a tulajdonosok akkor egy injector-ok kozt ertelmezett bejegyzessel
            // van dolgunk -> minden muveletnek exkluzivnak kell lennie
            // A Monitor.IsEntered() azert kell mert pl Singleton eseten a tulaj sosem lehet injector
            //

            if (UnderlyingContainer.IsDescendantOf(requested.Owner) && !Monitor.IsEntered(requested))
            {
                //
                // ServiceEntry-t zaroljuk h a lock injectorok kozt is ertelmezve legyen.
                // 

                lock (requested)
                {
                    //
                    // Singleton szerviz lehet hivva parhuzamosan szoval lehet kozben vki mar beallitotta.
                    //

                    if (requested.Built)
                        return requested.Instances.Single();

                    //
                    // - Az uj injector elettartama meg fogy egyezni a bejegyzes elettartamaval (mivel "requested.Owner"
                    //   gyermeke).
                    // - Ennek az injector peldanynak a felszabaditasa nem befolyasolja a szerviz elettartamat.
                    //

                    Injector dedicatedInjector = CreateScope(requested.Owner, FOptions);

                    try
                    {
                        return dedicatedInjector.Instantiate(requested);
                    }
                    catch
                    {
                        dedicatedInjector.Dispose();
                        throw;
                    }
                }
            }

            Debug.Assert(UnderlyingContainer == requested.Owner || UnderlyingContainer.Parent == requested.Owner, "Foreign entry");

            CheckBreaksTheRuleOfStrictDI(requested);

            IServiceReference result = new ServiceReference(requested, this);

            try
            {
                //
                // Az epp letrehozas alatt levo szerviz kerul az ut legvegere igy a fuggosegei
                // feloldasakor o lesz a szulo (FGraph.Requestor).
                //

                using (FGraph.With(result))
                {
                    FGraph.CheckNotCircular();

                    result.SetInstance();
                }

                return result;
            }
            catch
            {
                result.Release();
                throw;
            }
        }
    }
}
