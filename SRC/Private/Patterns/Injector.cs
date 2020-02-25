/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal class Injector : ServiceContainer, IInjectorEx
    {
        private readonly ServiceGraph FGraph;

        private readonly ServiceInstantiationStrategySelector FStrategySelector;

        private void CheckBreakTheRuleOfStrictDI(ServiceReference requestedRef) 
        {
            if (!Config.Value.Injector.StrictDI) return;

            AbstractServiceEntry
                requestor = FGraph.Current?.RelatedServiceEntry, // lehet NULL
                requested = requestedRef.RelatedServiceEntry;

            //
            // A kerelmezett szerviz tulajdonosanak egy szinten v feljebb kell lennie mint a kerelmezo 
            // tulajdonosa h biztosan legalabb annyi ideig letezzen mint a kerelmezo maga.
            //

            if (requestor?.Owner.IsDescendantOf(requested.Owner) == false) 
                throw new RequestNotAllowedException(requestor, requested, Resources.STRICT_DI);
        }

        private Injector(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions, ServiceGraph graph) : base(parent)
        {
            //
            // Injector nem hozhato letre absztrakt bejegyzesekkel.
            //

            Type[] abstractEntries = parent
                .Where(entry => entry.GetType() == typeof(AbstractServiceEntry))
                .Select(entry => entry.Interface)
                .ToArray();

            if (abstractEntries.Any())
            {
                //
                // "base" hivas miatt mar szukseges dispose-olni.
                //
                
                Dispose();

                var ioEx = new InvalidOperationException(Resources.INVALID_INJECTOR_ENTRY);
                ioEx.Data.Add(nameof(abstractEntries), abstractEntries);
                throw ioEx;
            }

            FactoryOptions = factoryOptions;
            FGraph = graph;
            FStrategySelector = new ServiceInstantiationStrategySelector(this);

            //
            // Felvesszuk sajat magunkat.
            //

            this.Instance<IInjector>(this, releaseOnDispose: false);
        }

        public Injector(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions = null) : this
        (
            Ensure.Parameter.IsNotNull(parent, nameof(parent)), 
            factoryOptions ?? new Dictionary<string, object>
            {
                { nameof(Config.Value.Injector.MaxSpawnedTransientServices), Config.Value.Injector.MaxSpawnedTransientServices }
            }, 
            new ServiceGraph()
        ) { }

        public ServiceReference GetReference(Type iface, string name)
        {
            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));
            Ensure.NotDisposed(this);

            //
            // Ha vkinek a fuggosege vagyunk akkor a fuggo szerviz itt meg nem lehet legyartva.
            //

            Debug.Assert(FGraph.Current?.Value == null, "Already produced services can not request dependencies");

            //
            // Bejegyzes lekerdezese.
            //

            AbstractServiceEntry entry = Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError);

            //
            // Szerviz peldany letrehozasa.
            //

            return FStrategySelector.GetStrategyFor(entry).Invoke(FGraph.Current /*requestor*/);
        }

        public IReadOnlyDictionary<string, object> FactoryOptions { get; }

        #region IInjectorEx
        public object Get(Type iface, string name) 
        {
            try
            {
                return GetReference(iface, name).Value;
            }

            //
            // Csak ott bovitjuk a kivetelt ahol az dobva volt (ez a metodus lehet rekurzivan hivva).
            //

            catch (ServiceNotFoundException e) when (e.Data["path"] == null)
            {
                e.Data["path"] = string.Join(" -> ", FGraph
                    .Select(node => (IServiceId) node.RelatedServiceEntry)
                    .Append(new ServiceId
                    {
                        Interface = iface,
                        Name = name
                    })
                    .Select(IServiceIdExtensions.FriendlyName));

                throw;
            }
        }

        public object TryGet(Type iface, string name) 
        {
            try
            {
                return Get(iface, name);
            }
            catch(ServiceNotFoundException) 
            {
                return null;
            }
        }

        public IServiceContainer UnderlyingContainer => this;

        void IInjectorEx.Instantiate(ServiceReference requested)
        {
            Ensure.NotDisposed(this);
            Ensure.AreEqual(requested?.RelatedInjector, this);        

            CheckBreakTheRuleOfStrictDI(requested);

            //
            // Az epp letrehozas alatt levo szerviz kerul az ut legvegere igy a fuggosegei
            // feloldasakor o lesz a szulo (FGraph.Current).
            //

            using (FGraph.With(requested))
            {
                FGraph.CheckNotCircular();

                requested.SetInstance(FactoryOptions);
            }
        }

        IInjectorEx IInjectorEx.CreateSibling(IServiceContainer parent)
        {
            Ensure.NotDisposed(this);

            return new Injector
            (
                parent,
                FactoryOptions,
                FGraph.CreateSubgraph()
            );
        }
        #endregion

        #region Composite
        public override IServiceContainer CreateChild() => throw new NotSupportedException();

        public override void AddChild(IServiceContainer child) => throw new NotSupportedException();

        public override void RemoveChild(IServiceContainer child) => throw new NotSupportedException();
        #endregion
    }
}