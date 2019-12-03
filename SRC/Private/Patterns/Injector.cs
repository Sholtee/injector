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

        private readonly Stack<(Type Interface, string Name)> FCurrentPath = new Stack<(Type Interface, string Name)>();

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

            (Type Interface, string Name) currentHop = (iface, name);
            FCurrentPath.Push(currentHop);

            try
            {
                //
                // Ha egynel tobbszor szerepel az aktualis szerviz akkor korkoros referenciank van.
                //

                if (FCurrentPath.Count(t => t == currentHop) > 1)
                    throw new CircularReferenceException(FCurrentPath);

                AbstractServiceEntry entry = Get(iface, name, QueryFlags);

                //
                // Lekerdezeshez mindig a deklaralo szervizkollekciobol csinalunk injector-t.
                //   - Igy a fuggosegek is a deklaralo kollekciobol lesznek feloldva (mellekhataskent Singleton szerviz peldaul
                //     hivatkozhat abstract fuggosegre, de az rossz konfiguracio).
                //   - Ujonan letrehozott injector eseten annak felszabaditasa a deklaralo kollekcio felszabaditasokor tortenik 
                //     (mivel annak a gyermeke lesz).
                //

                object instance = entry.GetService(() => entry.Owner == this ? this : new Injector(entry.Owner));

                //
                // Peldany tipusat ellenorizzuk mert a Factory(), Lazy() stb visszaadhat vicces dolgokat.
                //

                if (!iface.IsInstanceOfType(instance))
                    throw new Exception(string.Format(CultureInfo.CurrentCulture, Resources.INVALID_INSTANCE, iface));

                return instance;
            }
            finally
            {
                (Type Interface, string Name) removed = FCurrentPath.Pop();
                Debug.Assert(removed == currentHop);
            }
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