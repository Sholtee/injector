﻿/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal class Injector : ServiceContainer, IInjector
    {
        private readonly ServiceGraph FGraph = new ServiceGraph();
        private readonly AbstractServiceEntry FBoundEntry; // workaround

        public IReadOnlyDictionary<string, object> FactoryOptions { get; }

        private Injector(AbstractServiceEntry boundEntry, IReadOnlyDictionary<string, object> factoryOptions) 
            : this(boundEntry.Owner, factoryOptions)  => FBoundEntry = boundEntry;

        private bool BreaksTheRuleOfStrictDI(AbstractServiceEntry entry) 
        {
            if (!Config.Value.Injector.StrictDI) return false;

            IServiceContainer
                requestor = FGraph.Current?.RelatedServiceEntry.Owner,
                requested = entry.Owner;

            //
            // 1) Ha "parentOwner == null" akkor a lekderdezesi fa tetejen vagyunk
            // 2) Ha "entryOwner == null" akkor Instance-t kerdezunk le
            //

            if (requestor == null || requested == null) return false;

            //
            // A kerelmezett szerviz tulajdonosanak egy szinten v feljebb kell lennie mint a kerelmezo tulajdonosa h biztosan legalabb annyi
            // ideig letezzen mint a kerelmezo maga.
            //

            return !requestor.IsDescendantOf(requested);
        }

        public Injector(IServiceContainer parent, IReadOnlyDictionary<string, object> factoryOptions = null) : base(parent)
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

            //
            // Felvesszuk sajat magunkat.
            //

            this.Instance<IInjector>(this, releaseOnDispose: false);
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifetime of 'new Injector(...)' is managed by its parent container.")]
        public ServiceReference GetReference(Type iface, string name, QueryModes queryMode)
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
            // Bejegyzes lekerdezese, nyilt generikusok lezarasat mindenkepp megengedjuk.
            //

            AbstractServiceEntry entry = Get(iface, name, queryMode | QueryModes.AllowSpecialization);
            if (entry == null) return null;

            //
            // Ha korabban mar le lett gyartva a szerviz peldany akkor nincs dolgunk.
            //

            ServiceReference currentService = entry.Instance;

            if (currentService == null)
            {
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

                    var relatedInjector = new Injector(boundEntry: entry, FactoryOptions);

                    try
                    {
                        currentService = relatedInjector.GetReference(iface, name, QueryModes.Default);

                        //
                        // Mivel letezo bejegyzesunk van itt mar semmi kepp sem kaphatunk vissza NULL-t
                        //

                        Assert(currentService != null);
                    }
                    finally
                    {
                        if (currentService?.RelatedInjector != relatedInjector)
                            //
                            // Ket esetben juthatunk ide
                            //   1) Singleton peldanyt parhuzamosan megkiserelhet letrehozni tobb szal is ezert elofordulhat
                            //      h a legyartott injector nem is volt hasznalva.
                            //   2) Vmi gond volt szerviz hivatkozas feloldasa kozben (nem letezo hivatkozas pl.)
                            // Ilyenkor nincs ertelme megtartani az injectort.
                            //

                            relatedInjector.Dispose();
                    }
                } 
                else
                {
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
                }
            }

            //
            // Ha az aktualisan lekerdezett szerviz valakinek a fuggosege akkor hozzaadjuk a fuggosegi listahoz.
            //

            FGraph.AddAsDependency(currentService);

            return currentService;
        }

        #region IInjector
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The reference is released on container disposal.")]
        public virtual object Get(Type iface, string name) 
        {
            try
            {
                return GetReference(iface, name, QueryModes.ThrowOnError).Value;
            }
            catch (ServiceNotFoundException e)
            {
                IDictionary extra = e.Data;
                
                //
                // "@ -> nagyszulo -> szulo"
                //

                string 
                    prev = extra["requestor"] as string,
                    current = FGraph.Current?.RelatedServiceEntry.FriendlyName() ?? "@";

                extra["requestor"] = prev != null ? $"{current} -> {prev}" : current;

                throw;
            }
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The reference is released on container disposal.")]
        public virtual object TryGet(Type iface, string name) => GetReference(iface, name, QueryModes.Default)?.Value;

        public IServiceContainer UnderlyingContainer => this;
        #endregion

        #region Composite
        public override IServiceContainer CreateChild() => throw new NotSupportedException();

        public override void AddChild(IServiceContainer child) => throw new NotSupportedException();

        public override void RemoveChild(IServiceContainer child) => throw new NotSupportedException();
        #endregion
    }
}