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

        private bool BreaksTheRuleOfStrictDI(AbstractServiceEntry entry) 
        {
            if (!Config.Value.StrictDI) return false;

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
                //   * Ehhez az "entry.Owner?.IsDescendantOf(Parent)" feltetel helyes mivel injector letrehozasakor 
                //     mindig uj gyermek kontenert hozunk letre
                //   * Igy a fuggosegek is a deklaralo kollekciobol lesznek feloldva
                //     Mellekhatasok: 
                //       > Singleton szerviz hivatkozhat abstract fuggosegre (de az rossz konfiguracio).
                // - A referencia szamlalas miatt Singleton szerviz minden fuggosege is Singletonkent 
                //   viselkedik (ez ellen van a StrictDI).
                //

                if (entry.Owner?.IsDescendantOf(this.Parent) == false)
                {
                    Assert(entry.Owner != null);
                    Assert(entry.Lifetime >= Lifetime.Singleton);

                    //
                    // Az uj injector elettartama meg fogy egyezni a bejegyzes elettartamaval (mivel "entry.Owner"
                    // gyermeke). 
                    //

                    currentService = new Injector(entry.Owner).GetReference(iface, name);
                } 
                else
                {
                    if (BreaksTheRuleOfStrictDI(entry))
                        throw new RequestNotAllowedException(FGraph.Current.RelatedServiceEntry, entry, Resources.STRICT_DI);

                    //
                    // Letrehozunk egy ures referenciat h a szerviz legyartasa kozben a rekurziv GetReference()
                    // hivasokban lassak az (epp legyartas alatt levo) szulot.
                    //

                    currentService = new ServiceReference(entry);

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

                        if (!entry.SetInstance(this, currentService))
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

    public partial class Config 
    {
        /// <summary>
        /// Instructs the injector to throw if a service being requested has a dependency that should live shorter than the service should (e.g.: a <see cref="Lifetime.Singleton"/> service can not have <see cref="Lifetime.Transient"/> dependency).
        /// </summary>
        public bool StrictDI { get; set; } = false;
    }
}