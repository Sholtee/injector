/********************************************************************************
* Injector.Resolve.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal partial class Injector
    {
        private IServiceReference Resolve(AbstractServiceEntry requested)
        {
            //
            // 1. eset: Csak egy peldanyt kell letrehozni (Built == true) amit vki korabban mar megtett,
            //          akkor visszaadjuk azt.
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

                    Injector dedicatedInjector = CreateScope(requested.Owner, FOptions);

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
            // 3. eset: Uj peldanyt kell letrehozni es ez az injector peldany ezt megteheti
            //

            Debug.Assert(UnderlyingContainer == requested.Owner || UnderlyingContainer.Parent == requested.Owner, "Foreign entry");

            IServiceReference result = new ServiceReference(requested, this);

            try
            {
                //
                // Az epp letrehozas alatt levo szerviz kerul az ut legvegere igy a fuggosegei
                // feloldasakor o lesz a szulo (FGraph.Requestor).
                //

                using (FPath.With(result))
                {
                    FPath.CheckNotCircular();

                    result.SetInstance();
                    return result;
                }
            }
            catch
            {
                result.Release();
                throw;
            }
        }
    }
}
