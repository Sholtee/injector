/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if NETSTANDARD1_6
using System.Reflection;
#endif

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class Injector : ServiceContainer, IInjector
    {
        private readonly Stack<AbstractServiceReference> FGraph = new Stack<AbstractServiceReference>();

        private AbstractServiceReference ParentService => FGraph.Any() ? FGraph.Peek() : null;

        private Injector() => throw new NotSupportedException();

        public Injector(IServiceContainer parent) : base(parent) =>
            //
            // Felvesszuk sajat magunkat.
            //

            this.Instance<IInjector>(this, releaseOnDispose: false);

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

            Assert(ParentService?.Instance == null, "Already produced services can not request dependencies");

            //
            // Bejegyzes lekerdezese
            //

            AbstractServiceEntry entry = Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError);

            //
            // - Lekerdezeshez mindig a deklaralo szervizkollekciobol kell injector-t letrehozni.
            //   * Igy a fuggosegek is a deklaralo kollekciobol lesznek feloldva
            //     Mellekhatasok: 
            //       > Singleton szerviz hivatkozhat abstract fuggosegre (de az rossz konfiguracio).
            // - A referencia szamlalas miatt Singleton szerviz minden fuggosege is Singletonkent 
            //   viselkedik (ez ellen van a StrictDI).
            // - A lenti feltetel helyes mert injector letrehozasakor mindig uj gyermek kontener 
            //   hozunk letre (tehat pl Singleton szerviz tulajdonosa sose lesz az aktualis injector).
            //

            AbstractServiceReference currentService;

            if (entry.Owner?.IsDescendantOf(this.Parent) == false)
            {
                //
                // - Nem problema h minden egyes hivasnal uj injectort hozunk letre, az entry.GetService()
                //   legfeljebb nem fogja hasznalni.
                // - A referencia szamlalas miatt nem gond ha felszabaditjuk a szulo injectort.
                //

                using (var ownerInjector = new Injector(entry.Owner))
                {
                    currentService = ownerInjector.GetReference(iface, name);
                }

                Assert(!currentService.Disposed, "Service must not be disposed here");
            } 
            else 
            {  
                currentService = new ServiceReference(entry);

                //
                // A grafban egy szinttel lejebb levo elemek mind a mostani szervizunk fuggosegei (lasd metodus lezaras).
                //

                FGraph.Push(currentService);

                try
                {
                    //
                    // Ha egynel tobbszor szerepel az aktualis szerviz akkor korkoros referenciank van.
                    //

                    if (FGraph.LastIndexOf(currentService, ServiceReferenceComparer.Instance) > 0)
                    {
                        currentService.Release();

                        throw new CircularReferenceException(FGraph);
                    }

                    //
                    // Factory hivasa, innentol a ServiceEntry felelos a node felszabaditasaert.
                    //

                    entry.GetService(this, ref currentService);

                    Assert(currentService.Instance != null, "Instance was not set");

                    //
                    // Peldany tipusat ellenorizzuk mert a Factory(), Lazy() stb visszaadhat vicces dolgokat.
                    //

                    if (!iface.IsInstanceOfType(currentService.Instance))
                        throw new Exception(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, iface));
                }
                finally
                {
                    FGraph.Pop();
                }
            }

            //
            // Ha az aktualisan lekerdezett szerviz valakinek a fuggosege akkor hozzaadjuk a fuggosegi listahoz.
            //

            ParentService?.Dependencies.Add(currentService);

            return currentService;
        }

        #region IInjector
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The reference is released on container disposal.")]
        public object Get(Type iface, string name) => GetReference(iface, name).Instance;

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
        /// Instructs the injector to throw if a service being requested has one or more dependencies that have different owner than the service has (e.g.: a <see cref="Lifetime.Singleton"/> service can not have <see cref="Lifetime.Transient"/> dependency).
        /// </summary>
        public bool StrictDI { get; set; } = false;
    }
}