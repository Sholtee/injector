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

        private void CheckBreaksTheRuleOfStrictDI(ServiceReference requestedRef) 
        {
            if (!Config.Value.Injector.StrictDI) return;

            AbstractServiceEntry?
                requestor = FGraph.Current?.RelatedServiceEntry, // lehet NULL
                requested = requestedRef.RelatedServiceEntry;

            //
            // Ha vmelyik szulo hianyzik akkor a metodus nem ertelmezett
            //

            if (requestor?.Owner == null || requested.Owner == null) return;

            //
            // A kerelmezett szerviz tulajdonosanak egy szinten v feljebb kell lennie mint a kerelmezo 
            // tulajdonosa h biztosan legalabb annyi ideig letezzen mint a kerelmezo maga.
            //

            if (!requestor.Owner.IsDescendantOf(requested.Owner)) 
                throw new RequestNotAllowedException(requestor, requested, Resources.STRICT_DI);
        }

        protected Injector(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions, ServiceGraph graph) : base(parent)
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

        protected virtual Injector Spawn(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions, ServiceGraph graph) =>
            new Injector(parent, factoryOptions, graph);

        public Injector(IServiceContainer parent, IReadOnlyDictionary<string, object>? factoryOptions = null) : this
        (
            Ensure.Parameter.IsNotNull(parent, nameof(parent)), 
            factoryOptions ?? new Dictionary<string, object>
            {
                { nameof(Config.Value.Injector.MaxSpawnedTransientServices), Config.Value.Injector.MaxSpawnedTransientServices }
            }, 
            new ServiceGraph()
        ) { }

        public ServiceReference GetReference(Type iface, string? name)
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
            // Bejegyzes lekerdezese. A QueryModes.ThrowOnError miatt tuti nem NULL viszont h ne dumaljon a fordito
            // ellenorizzuk.
            //

            AbstractServiceEntry entry = Ensure.IsNotNull
            (
                Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError), 
                nameof(entry)
            );

            //
            // Szerviz peldany letrehozasa.
            //

            return FStrategySelector.GetStrategyFor(entry).Invoke(FGraph.Current /*requestor*/);
        }

        public IReadOnlyDictionary<string, object> FactoryOptions { get; }

        #region IInjectorEx
        public object Get(Type iface, string? name) 
        {
            try
            {
                return Ensure.IsNotNull
                (
                    GetReference(iface, name).Value,
                    nameof(ServiceReference.Value)
                );
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

        public object? TryGet(Type iface, string? name) 
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
            Ensure.Parameter.IsNotNull(requested, nameof(requested));
            Ensure.NotDisposed(this);
            Ensure.AreEqual(requested.RelatedInjector, this);        

            CheckBreaksTheRuleOfStrictDI(requested);

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

        IInjectorEx IInjectorEx.Spawn(IServiceContainer parent)
        {
            Ensure.NotDisposed(this);

            return Spawn
            (
                parent,
                FactoryOptions,
                FGraph.CreateSubgraph()
            );
        }
        #endregion

        #region Composite
        public override ICollection<IServiceContainer> Children { get; } = Array.Empty<IServiceContainer>();
        #endregion
    }
}