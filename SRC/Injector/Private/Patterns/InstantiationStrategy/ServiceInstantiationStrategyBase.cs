/********************************************************************************
* ServiceInstantiationStrategyBase.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal abstract class ServiceInstantiationStrategyBase
    {
        private static void CheckBreaksTheRuleOfStrictDI(IServiceGraph graph, AbstractServiceEntry requested)
        {
            if (!Config.Value.Injector.StrictDI) return;

            AbstractServiceEntry? requestor = graph.Requestor?.RelatedServiceEntry; // lehet NULL

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

        protected static IServiceReference Instantiate(IInjector injector, AbstractServiceEntry entry)
        {
            IServiceGraph graph = injector.Get<IServiceGraph>();

            CheckBreaksTheRuleOfStrictDI(graph, entry);

            IServiceReference result = new ServiceReference(entry, injector);

            try
            {
                //
                // Az epp letrehozas alatt levo szerviz kerul az ut legvegere igy a fuggosegei
                // feloldasakor o lesz a szulo (FGraph.Current).
                //

                using (graph.With(result))
                {
                    graph.CheckNotCircular();

                    result.SetInstance(injector.Options);
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
