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
    using Interfaces;
    using Properties;

    internal class Injector : ServiceContainer, IInjector, IScopeFactory
    {
        private readonly IServiceGraph FGraph;
            
        public Injector(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions, IServiceGraph graph) : base(parent)
        {
            Options = factoryOptions;
            FGraph = graph;
            
            UnderlyingContainer.Instance<IInjector>(this);
            UnderlyingContainer.Instance<IScopeFactory>(this);
            UnderlyingContainer.Instance(graph);
            UnderlyingContainer.Instance(parent);

            this.RegisterServiceEnumerator();
        }

        public Injector(IServiceContainer parent, IReadOnlyDictionary<string, object>? factoryOptions = null) : this
        (
            Ensure.Parameter.IsNotNull(parent, nameof(parent)),
            (factoryOptions ?? new Dictionary<string, object>()).Extend(nameof(Config.Value.Injector.MaxSpawnedTransientServices), Config.Value.Injector.MaxSpawnedTransientServices),
            new ServiceGraph()
        ){ }

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
        public IServiceReference GetReference(Type iface, string? name)
        {
            CheckNotDisposed();

            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            //
            // Ha vkinek a fuggosege vagyunk akkor a fuggo szerviz itt meg nem lehet legyartva.
            //

            Debug.Assert(FGraph.Requestor?.Value == null, "Already produced services can not request dependencies");

            const string path = nameof(path);

            try
            {
                //
                // Bejegyzes lekerdezese, generikus bejegyzes tipizalasat megengedjuk. A "QueryModes.ThrowOnError" 
                // miatt "entry" tuti nem NULL.
                //

                AbstractServiceEntry requestedEntry = Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError)!;

                IServiceReference? 
                    requestor = FGraph.Requestor,
                    requested = ServiceInstantiationStrategySelector.GetStrategyFor(this, requestedEntry).Invoke(requestor);

                return requested;
            }

            //
            // Csak ott bovitjuk a kivetelt ahol az dobva volt (ez a metodus lehet rekurzivan hivva).
            //

            catch (ServiceNotFoundException e) when (e.Data[path] == null)
            {
                e.Data[path] = string.Join(" -> ", FGraph
                    .Select(node => (IServiceId) node.RelatedServiceEntry)
                    .Append(new ServiceId(iface, name))
                    .Select(IServiceIdExtensions.FriendlyName));

                throw;
            }
        }

        public object Get(Type iface, string? name) 
        {
            CheckNotDisposed();

            object instance = GetReference(iface, name).GetEffectiveValue();

            if (!iface.IsInstanceOfType(instance))
                throw new InvalidCastException(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, iface));

            return instance;
        }

        public object? TryGet(Type iface, string? name) 
        {
            CheckNotDisposed();

            try
            {
                return Get(iface, name);
            }
            catch(ServiceNotFoundException) 
            {
                return null;
            }
        }

        public IServiceContainer UnderlyingContainer 
        {
            get
            {
                CheckNotDisposed();
                return this;
            }
        }

        public IReadOnlyDictionary<string, object> Options { get; }
        #endregion

        #region IScopeFactory
        IInjector IScopeFactory.CreateScope(IReadOnlyDictionary<string, object>? options) => new Injector(Parent!, options);

        IInjector IScopeFactory.CreateScope(IServiceContainer parent, IServiceGraph node, IReadOnlyDictionary<string, object>? options) => new Injector
        (
            Ensure.Parameter.IsNotNull(parent, nameof(parent)), 
            options ?? new Dictionary<string, object>(0), 
            Ensure.Parameter.IsNotNull(node, nameof(node))
        );
        #endregion

        #region Composite
        public override ICollection<IServiceContainer> Children { get; } = Array.Empty<IServiceContainer>();
        #endregion
    }
}