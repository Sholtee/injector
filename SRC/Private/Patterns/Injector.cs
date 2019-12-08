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

        public Injector(IServiceContainer parent): base(parent) =>
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

            var currentNode = new ServiceReference(iface, name);

            //
            // A grafban egy szinttel lejebb levo elemek mind a mostani szervizunk fuggosegei.
            //

            FGraph.Push(currentNode);

            try
            {
                //
                // Ha egynel tobbszor szerepel az aktualis szerviz akkor korkoros referenciank van.
                //

                if (FGraph.Count(node => (node.Interface, node.Name) == (currentNode.Interface, currentNode.Name)) > 1)
                    throw new CircularReferenceException(FGraph.Select(node => (node.Interface, node.Name)));

                AbstractServiceEntry entry = Get(iface, name, QueryFlags);

                //
                // - Lekerdezeshez mindig a deklaralo szervizkollekciobol csinalunk injector-t.
                //   * Igy a fuggosegek is a deklaralo kollekciobol lesznek feloldva
                //     Mellekhatasok: 
                //       > Singleton szerviz hivatkozhat abstract fuggosegre (de az rossz konfiguracio).
                //       > Singleton szerviz Scoped fuggosege is Singletonkent viselkedik (szinten rossz konfiguracio).
                //   * Ujonan letrehozott injector eseten annak felszabaditasa a deklaralo kollekcio felszabaditasokor tortenik 
                //     (mivel annak a gyermeke lesz).
                //
                // - Visszakaphatunk mar korabban legyartott referenciat is (pl Singleton szerviz eseten) ezert felulirjuk
                //   a "currentNode" valtozot (a regi valtozo mar nem lesz hasznalva)
                //

                currentNode = entry.GetService(() => entry.Owner == this ? this : new Injector(entry.Owner), currentNode);

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