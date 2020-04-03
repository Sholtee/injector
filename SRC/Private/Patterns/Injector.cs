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

    internal class Injector : ServiceContainer, IInjector
    {
        #region Private
        private readonly ServiceInstantiationStrategySelector FStrategySelector;

        private readonly ServiceGraph FGraph;

        private void CheckBreaksTheRuleOfStrictDI(ServiceReference requestedRef) 
        {
            if (!Config.Value.Injector.StrictDI) return;

            AbstractServiceEntry?
                requestor = FGraph.Current?.RelatedServiceEntry, // lehet NULL
                requested = requestedRef.RelatedServiceEntry;

            //
            // Ha a fuggosege fa gyokerenel vagyunk akkor a metodus nem ertelmezett.
            //

            if (requestor == null) return;

            //
            // A kerelmezett szerviz tulajdonosanak egy szinten v feljebb kell lennie mint a kerelmezo 
            // tulajdonosa h biztosan legalabb annyi ideig letezzen mint a kerelmezo maga.
            //

            if (!requestor.Owner.IsDescendantOf(requested.Owner)) 
                throw new RequestNotAllowedException(requestor, requested, Resources.STRICT_DI);
        }
        #endregion

        #region Protected
        protected ServiceGraph CreateSubgraph() => FGraph.CreateSubgraph();

        protected Injector(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions, ServiceGraph graph) : base(parent)
        {
            FactoryOptions = Ensure.Parameter.IsNotNull(factoryOptions, nameof(factoryOptions));
            FGraph = Ensure.Parameter.IsNotNull(graph, nameof(graph));

            FStrategySelector = new ServiceInstantiationStrategySelector(this);

            this.RegisterSelf();
            this.RegisterServiceEnumerator();
        }
        #endregion

        #region Internals
        internal virtual void Instantiate(ServiceReference requested) // [jelenleg] csak a tesztek miatt kell virtualis legyen
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

        internal virtual Injector Spawn(IServiceContainer parent)
        {
            Ensure.Parameter.IsNotNull(parent, nameof(parent));
            Ensure.NotDisposed(this);

            return new Injector(parent, FactoryOptions, CreateSubgraph());
        }

        internal virtual ServiceReference GetReference(Type iface, string? name)
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
            // Bejegyzes lekerdezese, generikus bejegyzes tipizalasat megengedjuk. A "QueryModes.ThrowOnError" 
            // miatt "entry" tuti nem NULL.
            //

            AbstractServiceEntry entry = Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError)!;

            //
            // Szerviz peldany letrehozasa. 
            //

            return FStrategySelector
                .GetStrategyFor(entry)
                .Invoke(FGraph.Current /*requestor*/);
        }
        #endregion

        public Injector(IServiceContainer parent, IReadOnlyDictionary<string, object>? factoryOptions = null) : this
        (
            Ensure.Parameter.IsNotNull(parent, nameof(parent)),
            factoryOptions ?? new Dictionary<string, object>
            {
                { nameof(Config.Value.Injector.MaxSpawnedTransientServices), Config.Value.Injector.MaxSpawnedTransientServices }
            },
            new ServiceGraph()
        ){ }

        public IReadOnlyDictionary<string, object> FactoryOptions { get; }

        public override void Add(AbstractServiceEntry entry)
        {
            Ensure.Parameter.IsNotNull(entry, nameof(entry));

            //
            // Injector nem hasznalhato absztrakt bejegyzesekkel.
            //

            if (entry.GetType() == typeof(AbstractServiceEntry))
            {
                var ioex = new InvalidOperationException(Resources.INVALID_INJECTOR_ENTRY);
                ioex.Data[nameof(entry)] = entry;

                throw ioex;
            }

            base.Add(entry);
        }

        #region IInjector
        public object Get(Type iface, string? name) 
        {
            try
            {
                ServiceReference reference = GetReference(iface, name);

                return reference.Value!;
            }

            //
            // Csak ott bovitjuk a kivetelt ahol az dobva volt (ez a metodus lehet rekurzivan hivva).
            //

            catch (ServiceNotFoundException e) when (e.Data["path"] == null)
            {
                e.Data["path"] = string.Join(" -> ", FGraph
                    .Select(node => (IServiceId) node.RelatedServiceEntry)
                    .Append(new ServiceId(iface, name))
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
        #endregion

        #region Composite
        public override ICollection<IServiceContainer> Children { get; } = Array.Empty<IServiceContainer>();
        #endregion
    }
}