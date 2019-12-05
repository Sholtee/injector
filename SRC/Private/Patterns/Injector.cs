/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly Stack<ServiceReferenceHolder> FGraph = new Stack<ServiceReferenceHolder>();

        private Injector() => throw new NotSupportedException();

        public Injector(IServiceContainer parent): base(parent) =>
            //
            // Felvesszuk sajat magunkat.
            //

            this.Instance<IInjector>(this, releaseOnDispose: false);

        #region IInjector
        public object Get(Type iface, string name, Type target)
        {
            if (iface == null)
                throw new ArgumentNullException(nameof(iface));

            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            if (iface.IsGenericTypeDefinition())
                throw new ArgumentException(Resources.CANT_INSTANTIATE_GENERICS, nameof(iface));

            if (target != null && !target.IsClass())
                throw new ArgumentException(Resources.NOT_A_CLASS, nameof(target));

            var currentNode = new ServiceReferenceHolder
            {
                Interface = iface,
                Name = name,
                Value = new ServiceReference() // valtozhat
            };

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
                // - A szerviz legyartasakor a referencia valtozhat ezert felulirjuk az aktualis csomopontban.
                //

                currentNode.Value = entry.GetService(() => entry.Owner == this ? this : new Injector(entry.Owner), currentNode.Value);

                Debug.Assert(currentNode.Value.Instance != null, "Instance was not set");

                //
                // Peldany tipusat ellenorizzuk mert a Factory(), Lazy() stb visszaadhat vicces dolgokat.
                //

                if (!iface.IsInstanceOfType(currentNode.Value.Instance))
                    throw new Exception(string.Format(CultureInfo.CurrentCulture, Resources.INVALID_INSTANCE, iface));

                //
                // Ha kivetel volt nem kell semmit sem felszabaditani mert a szerviz bejegyzesek dispose-olasakor
                // minden legyartott szerviznek is fel kene szabadulnia.
                //
            }
            finally
            {
                ServiceReferenceHolder removed = FGraph.Pop();
                Debug.Assert(removed == currentNode);
            }

            //
            // Ha az aktualisan lekerdezett szerviz valakinek a fuggosege akkor hozzaadjuk a fuggosegi listahoz.
            //

            if (FGraph.Any()) 
                FGraph
                    .Peek()
                    .Value
                    .Dependencies
                    .Add(currentNode.Value);

            return currentNode.Value.Instance;
        }

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