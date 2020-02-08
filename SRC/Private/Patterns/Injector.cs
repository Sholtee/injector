/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
#if NETSTANDARD1_6
using System.Reflection;
#endif

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class Injector : ServiceContainer, IInjector
    {
        private readonly ServiceGraph FGraph = new ServiceGraph();

        private Injector() => throw new NotSupportedException();

        private object Trigger { get; }  // workaround

        private Injector(AbstractServiceEntry trigger) : this(trigger.Owner) => Trigger = trigger;

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

        public Injector(IServiceContainer parent) : base(parent) =>
            //
            // Felvesszuk sajat magunkat.
            //

            this.Instance<IInjector>(this, releaseOnDispose: false);

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Lifetime of 'new Injector(...)' is managed by its parent container.")]
        public AbstractServiceReference GetReference(Type iface, string name)
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
            // Bejegyzes lekerdezese
            //

            AbstractServiceEntry entry = Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError);

            //
            // Ha korabban mar le lett gyartva a szerviz peldany akkor nincs dolgunk.
            //

            AbstractServiceReference currentService = entry.Instance;

            if (currentService == null)
            {
                //
                // - Lekerdezeshez mindig a tulajdonos szervizkollekciobol kell injector-t letrehozni.
                //   * Igy a fuggosegek is a deklaralo kollekciobol lesznek feloldva. Mellekhataskent 
                //     Singleton szerviz hivatkozhat abstract fuggosegre (de az rossz konfiguracio).
                //   * A Trigger-es csoda egy korbedolgozas mivel Singleton bejegyzes tulajdonosa sose 
                //     lehet az injector maga.
                // - A referencia szamlalas miatt Singleton szerviz minden fuggosege is Singletonkent 
                //   viselkedik (ez ellen van a StrictDI).
                //

                if (entry.Owner != null && entry.Owner != this && entry != Trigger)
                {
                    Assert(entry.Lifetime >= Lifetime.Singleton);

                    //
                    // - Az uj injector elettartama meg fogy egyezni a bejegyzes elettartamaval (mivel "entry.Owner"
                    //   gyermeke).
                    // - Az eredeti (felhasznalo altal hivott) injector felszabaditasa nem befolyasolja a szerviz 
                    //   elettartamat.
                    //

                    var relatedInjector = new Injector(trigger: entry);

                    currentService = relatedInjector.GetReference(iface, name);

                    if (currentService.RelatedInjector != relatedInjector)
                        //
                        // Singleton peldanyt parhuzamosan megkiserelhet letrehozni tobb szal is ezert elofordulhat
                        // h a legyartott injector nem is volt hasznalva, ilyenkor felszabaditjuk.
                        //

                        relatedInjector.Dispose();
                } 
                else
                {
                    if (BreaksTheRuleOfStrictDI(entry))
                        throw new RequestNotAllowedException(FGraph.Current.RelatedServiceEntry, entry, Resources.STRICT_DI);

                    //
                    // Letrehozunk egy ures referenciat h a szerviz legyartasa kozben a rekurziv GetReference()
                    // hivasokban lassak az (epp legyartas alatt levo) szulot.
                    //

                    currentService = new ServiceReference(entry, this);

                    using (FGraph.With(currentService))
                    {
                        //
                        // Ellenorizzuk h nem volt e korkoros referencia.
                        //

                        if (FGraph.CircularReference)
                        {
                            currentService.Release();
                            throw new CircularReferenceException(FGraph);
                        }

                        //
                        // Factory hivasa, innentol a ServiceEntry felelos a szerviz peldany felszabaditasaert.
                        //

                        if (!currentService.SetInstance())
                        {
                            //
                            // - Valaki korabban mar beallitotta (parhuzamos eset Singleton elettartamnal). 
                            // - Nem gond h With() blokkban vagyunk siman felszabadithato az entitas.
                            //

                            currentService.Release();
                            currentService = entry.Instance;

                            Assert(currentService != null);
                        }

                        Assert(currentService.Value != null, "Instance was not set");

                        //
                        // Peldany tipusat ellenorizzuk mert a Factory(), Lazy() stb visszaadhat vicces dolgokat.
                        //

                        if (!iface.IsInstanceOfType(currentService.Value))
                            throw new Exception(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, iface));
                    }
                }
            }

            //
            // Ha az aktualisan lekerdezett szerviz valakinek a fuggosege akkor hozzaadjuk a fuggosegi listahoz.
            //

            FGraph.Add(currentService);

            return currentService;
        }

        #region IInjector
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The reference is released on container disposal.")]
        public object Get(Type iface, string name) => GetReference(iface, name).Value;

        public IServiceContainer UnderlyingContainer => this;
        #endregion

        #region Composite
        public override IServiceContainer CreateChild() => throw new NotSupportedException();

        public override void AddChild(IServiceContainer child) => throw new NotSupportedException();

        public override void RemoveChild(IServiceContainer child) => throw new NotSupportedException();
        #endregion
    }
}