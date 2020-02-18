/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal class Injector : ServiceContainer, IInjector
    {
        private readonly ServiceGraph FGraph;
        private readonly AbstractServiceEntry FBoundEntry; // workaround

        public IReadOnlyDictionary<string, object> FactoryOptions { get; }

        private Injector(AbstractServiceEntry boundEntry, Injector creator) : this(boundEntry.Owner, creator.FactoryOptions, creator.FGraph)
        {
            FBoundEntry = boundEntry;
        }

        private bool BreaksTheRuleOfStrictDI(AbstractServiceEntry entry) 
        {
            if (!Config.Value.Injector.StrictDI) return false;

            IServiceContainer
                requestor = FGraph.Current?.RelatedServiceEntry.Owner,
                requested = entry.Owner;

            //
            // 1) Ha "requestor == null" akkor a lekderdezesi fa tetejen vagyunk
            // 2) Ha "requested == null" akkor Instance-t kerdezunk le
            //

            if (requestor == null || requested == null) return false;

            //
            // A kerelmezett szerviz tulajdonosanak egy szinten v feljebb kell lennie mint a kerelmezo tulajdonosa h biztosan legalabb annyi
            // ideig letezzen mint a kerelmezo maga.
            //

            return !requestor.IsDescendantOf(requested);
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

            FactoryOptions = factoryOptions ?? new Dictionary<string, object>
            {
                { nameof(Config.Value.Injector.MaxSpawnedTransientServices), Config.Value.Injector.MaxSpawnedTransientServices }
            };

            FGraph = graph?.CreateSubgraph() ?? new ServiceGraph();

            //
            // Felvesszuk sajat magunkat.
            //

            this.Instance<IInjector>(this, releaseOnDispose: false);
        }

        public Injector(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions = null) : this(parent, factoryOptions, null) { }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifetime of 'new Injector(...)' is managed by its parent container.")]
        public ServiceReference GetReference(Type iface, string name)
        {
            CheckDisposed();

            if (iface == null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            if (iface.IsGenericTypeDefinition())
                throw new ArgumentException(Resources.CANT_INSTANTIATE_GENERICS, nameof(iface));

            //
            // Ha vkinek a fuggosege vagyunk akkor a fuggo szerviz itt meg nem lehet legyartva.
            //

            Assert(FGraph.Current?.Value == null, "Already produced services can not request dependencies");

            //
            // Bejegyzes lekerdezese.
            //

            AbstractServiceEntry entry = Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError);

            //
            // 1. ESET: Korabban mar le lett gyartva a szerviz.
            //
            // Felvesszuk az uj fuggoseget es viszlat.
            //

            ServiceReference currentService = entry.Instance;

            if (currentService != null) 
            {
                FGraph.AddAsDependency(currentService);
                return currentService;
            }

            //
            // 2. ESET: Nem az injector a tulajdonos.
            //
            // - Lekerdezeshez mindig a tulajdonos szervizkollekciobol kell injector-t letrehozni.
            //   * Igy a fuggosegek is a deklaralo kollekciobol lesznek feloldva. Mellekhataskent 
            //     Singleton szerviz hivatkozhat abstract fuggosegre (de az rossz konfiguracio).
            //   * A BoundEntry-s csoda egy korbedolgozas mivel Singleton bejegyzes tulajdonosa 
            //     sose lehet az injector maga.
            // - A referencia szamlalas miatt Singleton szerviz minden fuggosege is Singletonkent 
            //   viselkedik (ez ellen van a StrictDI).
            //

            if (entry.Owner != null && entry.Owner != this && entry != FBoundEntry)
            {
                Assert(entry.Lifetime >= Lifetime.Singleton);

                //
                // - Az uj injector elettartama meg fogy egyezni a bejegyzes elettartamaval (mivel "entry.Owner"
                //   gyermeke).
                // - Az eredeti (felhasznalo altal hivott) injector felszabaditasa nem befolyasolja a szerviz 
                //   elettartamat.
                //

                var relatedInjector = new Injector(boundEntry: entry, this);

                try
                {
                    currentService = relatedInjector.GetReference(iface, name);
                }
                catch
                {
                    relatedInjector.Dispose();
                    throw;
                }

                //
                // Ha vki megelozott minket es mar legyartota a szervizt (pl Singleton peldanyt parhuzamos
                // esetben) akkor az 1. ESET lep eletbe. Ilyenkor az uj injector peldanyra mar nincs szuksegunk.
                //

                if (currentService.RelatedInjector != relatedInjector) 
                {
                    relatedInjector.Dispose();

                    FGraph.AddAsDependency(currentService);
                }

                //
                // Kulomben a fuggosegi grafot nem kell boviteni mert a "relatedInjector" mar megtette.
                //

                return currentService;
            }

            //
            // 3. ESET: Az injector a tulajdonos, o peldanyosit.
            //

            //
            // Ellenorizzuk h nem ejtenenk e fogsagba az ujonan letrehozott szervizt
            //

            if (BreaksTheRuleOfStrictDI(entry))
                throw new RequestNotAllowedException(FGraph.Current.RelatedServiceEntry, entry, Resources.STRICT_DI);

            //
            // Letrehozunk egy ures referenciat h a szerviz legyartasa kozben a rekurziv GetReference()
            // hivasokban is lassuk az (epp legyartas alatt levo) szulot.
            //

            using (FGraph.With(currentService = new ServiceReference(entry, this)))
            {
                FGraph.CheckNotCircular();

                //
                // Ha a peldany beallitasa sikeres onnantol a "RelatedServiceEntry" felelos a hivatkozas felszabaditasaert.
                //

                if (currentService.SetInstance(FactoryOptions)) 
                    currentService.AddRef(); // AddRef() nelkul a using-ot elhagyva felszabadulna a hivatkozas

                //
                // Ha a peldany beallitas sikertelen az azt jelenti h valaki korabban mar beallitotta (parhuzamos eset 
                // Singleton elettartamnal).
                //

                else 
                    currentService = entry.Instance;
            }

            //
            // Ha az aktualisan lekerdezett szerviz valakinek a fuggosege akkor hozzaadjuk a fuggosegi listahoz.
            //

            FGraph.AddAsDependency(currentService);

            return currentService;
        }

        #region IInjector
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The reference is released on container disposal.")]
        public object Get(Type iface, string name) 
        {
            try
            {
                return GetReference(iface, name).Value;
            }
            catch (ServiceNotFoundException e)
            {
                const string path = nameof(path);

                //
                // "nagyszulo -> szulo -> gyerek"
                //

                string
                    prev = e.Data[path] as string,
                    current = new ServiceId { Interface = iface, Name = name }.FriendlyName();

                e.Data[path] = prev != null ? $"{current} -> {prev}" : current;

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
        #endregion

        #region Composite
        public override IServiceContainer CreateChild() => throw new NotSupportedException();

        public override void AddChild(IServiceContainer child) => throw new NotSupportedException();

        public override void RemoveChild(IServiceContainer child) => throw new NotSupportedException();
        #endregion
    }
}