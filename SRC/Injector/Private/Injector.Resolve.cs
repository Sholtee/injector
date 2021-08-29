/********************************************************************************
* Injector.Resolve.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Threading;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal partial class Injector
    {
        internal IServiceReference Resolve(AbstractServiceEntry requested)
        {
            //
            // 1. eset: Csak egy peldanyt kell letrehozni amit vki korabban mar megtett [HasFlag(Built)],
            //          visszaadjuk azt.
            //

            if (requested.State.HasFlag(ServiceEntryStates.Built))
                return requested.Instances.Single();

            //
            // 2. eset: Uj peldanyt kell letrehozni de nem mi vagyunk a bejegyzes tulajdonosai (megosztott bejegyzes)
            //          ezert letre kell hozni egy dedikalt injector-t.
            //          Megjegyzesek:
            //            - A nem birtokolt bejegyzesek injector peldanyok kozt ertelmezettek ezert minden muveletnek exkluzivnak kell
            //              lennie.
            //            - A Monitor.IsEntered() vizsgalat azert kell h lassuk ha az aktualis szal korabban mar elkezdte feldolgozni a
            //              szerviz igenylest (nem mellesleg a lock(...) rekurzio eseten nem blokkolodna, igy ez a megoldas jol kezeli
            //              azt az esetet is ha kontener altal kezelt elettartamu bejegyzes hivatkozik sajat magara -> nem lesz S.O.E.).
            //

            if (requested.IsShared && !Monitor.IsEntered(requested))
            {
                //
                // ServiceEntry-t zaroljuk h a lock injectorok kozt is ertelmezve legyen.
                // 

                lock (requested)
                {
                    //
                    // A lock miatt lehet h kozben vki mar beallitotta.
                    //

                    if (requested.State.HasFlag(ServiceEntryStates.Built))
                        return requested.Instances.Single();

                    //
                    // Letrehozunk egy dedikalt injector-t aminek a felszabaditasa "requested.Owner" feladata lesz
                    //   -> Elettartama meg fog egyezni a bejegyzes elettartamaval (container controlled lifetime)
                    //   -> Ennek az injector peldanynak a felszabaditasa nem befolyasolja a szerviz elettartamat.
                    //

                    Injector dedicatedInjector = CreateScope(requested.Owner);

                    try
                    {
                        //
                        // Ugrunk a 3. esetre
                        //

                        return dedicatedInjector.Resolve(requested);
                    }
                    catch
                    {
                        dedicatedInjector.Dispose();
                        throw;
                    }
                }
            }

            //
            // 3. eset: Uj peldanyt kell letrehozni es ezt az injector peldany megteheti
            //

            Assert(UnderlyingContainer == requested.Owner || UnderlyingContainer.Parent == requested.Owner, "Foreign entry");

            //
            // A result.Value itt meg ures, a SetInstance() allitja be
            //

            IServiceReference result = new ServiceReference(requested, this);

            try
            {
                //
                // Az epp letrehozas alatt levo szerviz kerul az ut legvegere igy a fuggosegei
                // feloldasakor o lesz a szulo (FGraph.Requestor).
                //

                FPath.Push(result);
                try
                {
                    FPath.CheckNotCircular();

                    result.SetInstance();
                    return result;
                }
                finally
                {
                    FPath.Pop();
                }
            }
            catch
            {
                result.Release();
                throw;
            }
        }

        internal IServiceReference? Resolve(Type iface, string? name, QueryModes queryModes)
        {
            //
            // Ha vkinek a fuggosege vagyunk akkor a fuggo szerviz itt meg nem lehet legyartva.
            //

            Assert(FPath.Last?.Value is null, "Already produced services can not request dependencies");

            AbstractServiceEntry? requested;

            try
            {
                requested = Get(iface, name, queryModes);
            }
            catch (ServiceNotFoundException e)
            {
                //
                // Ha az igenyelt szerviz valakinek a fuggosege akkor az utvonal mar biztosan nem ures.
                //

                e.Data["path"] = ServicePath.Format
                (
                    FPath
                        .Select(svc => (IServiceId) svc.RelatedServiceEntry)
                        .Append(new ServiceId(iface, name))
                );
                throw;
            }

            if (requested is null)
                return null;

            if (Config.Value.Injector.StrictDI)
            {
                AbstractServiceEntry? requestor = FPath.Last?.RelatedServiceEntry;

                //
                // - Ha a fuggosegi fa gyokerenel vagyunk akkor a metodus nem ertelmezett.
                // - A kerelmezett szerviznek legalabb addig kell leteznie mint a kerelmezo szerviznek.
                //

                if (requestor is not null && requested.Lifetime!.CompareTo(requestor.Lifetime!) < 0)
                {
                    var ex = new RequestNotAllowedException(Resources.STRICT_DI);
                    ex.Data["requestor"] = requestor;
                    ex.Data["requested"] = requested;

                    throw ex;
                }
            }

            //
            // Fuggosegek feloldasa es peldanyositas (ez a metodus rekurzivan ismet meghivasra kerulhet)
            //

            IServiceReference resolved = Resolve(requested);

            //
            // Minden fuggoseget megtalaltunk, a szerviz sikeresen peldanyositasra kerult.
            // Ha a szervizt egy masik szerviz fuggosege akkor felvesszuk annak fuggosegi listajaba.
            //

            FPath.Last?.AddDependency(resolved);
            return resolved;
        }
    }
}
