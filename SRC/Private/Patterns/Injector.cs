/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
#if NETSTANDARD1_6
using System.Reflection;
#endif

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class Injector : ServiceContainer, IInjector
    {
        private const QueryModes QueryFlags = QueryModes.AllowSpecialization | QueryModes.ThrowOnError;

        private readonly Stack<ServiceReference> FGraph = new Stack<ServiceReference>();

        private Injector() => throw new NotSupportedException();

        public Injector(IServiceContainer parent) : base(parent) =>
            //
            // Felvesszuk sajat magunkat.
            //

            this.Instance<IInjector>(this, releaseOnDispose: false);

        public ServiceReference GetReference(Type iface, string name)
        {
            if (iface == null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            if (iface.IsGenericTypeDefinition())
                throw new ArgumentException(Resources.CANT_INSTANTIATE_GENERICS, nameof(iface));

            AbstractServiceEntry entry = Get(iface, name, QueryFlags);

            //
            // - Lekerdezeshez mindig a deklaralo szervizkollekciobol kell injector-t letrehozni.
            //   * Igy a fuggosegek is a deklaralo kollekciobol lesznek feloldva
            //     Mellekhatasok: 
            //       > Singleton szerviz hivatkozhat abstract fuggosegre (de az rossz konfiguracio).
            // - A referencia szamlalas miatt Singleton szerviz minden fuggosege is Singletonkent 
            //   viselkedik.
            //

            ServiceReference currentNode;

            if (entry.Owner != this && entry.Owner != this.Parent)
            {
                //
                // - Nem problema h minden egyes hivasnal uj injectort hozunk letre, az entry.GetService()
                //   legfeljebb nem fogja hasznalni.
                // - A referencia szamlalas miatt nem gond ha felszabaditjuk a szulo injectort.
                //

                using (var ownerInjector = new Injector(entry.Owner))
                {
                    currentNode = ownerInjector.GetReference(iface, name);
                }

                Debug.Assert(!currentNode.Disposed, "Node already disposed");
            } 
            else 
            {  
                currentNode = new ServiceReference(iface, name);

                //
                // A grafban egy szinttel lejebb levo elemek mind a mostani szervizunk fuggosegei (lasd metodus lezaras).
                //

                FGraph.Push(currentNode);

                try
                {
                    //
                    // Ha egynel tobbszor szerepel az aktualis szerviz akkor korkoros referenciank van.
                    //

                    if (FGraph.Count(node => (node.Interface, node.Name) == (currentNode.Interface, currentNode.Name)) > 1)
                        throw new CircularReferenceException(FGraph.Select(node => (node.Interface, node.Name)));

                    //
                    // Factory hivasa
                    //

                    entry.GetService(this, ref currentNode);

                    Debug.Assert(currentNode.Instance != null, "Instance was not set");

                    //
                    // Peldany tipusat ellenorizzuk mert a Factory(), Lazy() stb visszaadhat vicces dolgokat.
                    //

                    if (!iface.IsInstanceOfType(currentNode.Instance))
                        throw new Exception(string.Format(CultureInfo.CurrentCulture, Resources.INVALID_INSTANCE, iface));

                    //
                    // Ha kivetel volt nem kell semmit sem felszabaditani mert a szerviz bejegyzesek dispose-olasakor
                    // minden legyartott szerviznek is fel kene szabadulnia.
                    //
                }
                finally
                {
                    FGraph.Pop();
                }
            }

            //
            // Ha az aktualisan lekerdezett szerviz valakinek a fuggosege akkor hozzaadjuk a fuggosegi listahoz.
            //

            if (FGraph.Any()) 
                FGraph
                    .Peek()
                    .Dependencies
                    .Add(currentNode);

            return currentNode;
        }

        #region IInjector
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The reference is released on container disposal.")]
        public object Get(Type iface, string name) => GetReference(iface, name).Instance;

        public Lifetime? LifetimeOf(Type iface, string name)
        {
            if (iface == null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            return Get(iface, name, QueryFlags).Lifetime;
        }
        #endregion

        #region Composite
        public override IServiceContainer CreateChild() => throw new NotSupportedException();

        public override void AddChild(IServiceContainer child) => throw new NotSupportedException();

        public override void RemoveChild(IServiceContainer child) => throw new NotSupportedException();
        #endregion
    }
}