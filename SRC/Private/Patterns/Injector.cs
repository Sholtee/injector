/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if NETSTANDARD1_6
using System.Reflection;
#endif

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class Injector : ServiceContainer, IInjector
    {
        private readonly Stack<(Type Interface, string Name)> FCurrentPath = new Stack<(Type Interface, string Name)>();

        private Injector() => throw new NotSupportedException();

        public Injector(IServiceContainer parent): base(parent)
        {
            //
            // Felvesszuk sajat magunkat.
            //

            Add(new InstanceServiceEntry
            (
                @interface: typeof(IInjector),
                name: null,
                value: this,
                releaseOnDispose: false,
                owner: this
            ));
        }

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

            //
            // Ha az OnServiceRequest esemenyben visszakaptunk szerviz peldanyt akkor visszaadjuk azt.
            //

            var ev = new InjectorEventArg(iface, name, target);
            OnServiceRequest?.Invoke(this, ev);
            if (ev.Service != null) return ev.Service;

            (Type Interface, string Name) currentHop = (iface, name);
            FCurrentPath.Push(currentHop);

            try
            {
                //
                // Ha egynel tobbszor szerepel az aktualis szerviz akkor korkoros referenciank van.
                //

                if (FCurrentPath.Count(t => t == currentHop) > 1)
                {
                    IEnumerable<string> friendlyPath = FCurrentPath.Select(cp => 
                    {
                        string result = cp.Interface.FullName;
                        if (cp.Name != null) result += $":{cp.Name}";
                        return result;
                    });

                    throw new InvalidOperationException(string.Format(Resources.CIRCULAR_REFERENCE, string.Join(" -> ", friendlyPath)));
                }

                AbstractServiceEntry entry = Get(iface, name, QueryMode.AllowSpecialization | QueryMode.ThrowOnError);

                //
                // Lekerdezeshez mindig a deklaralo szervizkollekciobol csinalunk injector-t.
                //   - Igy a fuggosegek is a deklaralo kollekciobol lesznek feloldva.
                //   - Ujonan letrehozott injector eseten annak felszabaditasa a deklaralo kollekcio felszabaditasokor tortenik 
                //     (mivel annak a gyermeke lesz).
                //

                object instance = entry.GetService(() => entry.Owner == this ? this : new Injector(entry.Owner));

                //
                // Peldany tipusat ellenorizzuk mert a Factory(), Lazy() stb visszaadhat vicces dolgokat.
                //

                if (!iface.IsInstanceOfType(instance))
                    throw new Exception(string.Format(Resources.INVALID_INSTANCE, iface));

                //
                // Ha az OnServiceRequested esemenyben felulirjak a szervizt akkor azt
                // adjuk vissza.
                //

                ev.Service = instance;
                OnServiceRequested?.Invoke(this, ev);

                return ev.Service;
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

            //
            // Az OnServiceRequest esemenyben visszaadhatunk olyan szervizt is ami nem volt regisztralva.
            // Ez a tortenet viszont a kliens szamara lathatatlan -> megprobalhatja az elettartamat
            // lekerdezni, ekkor ne legyen kivetel.
            //

            return Get(iface, name, QueryMode.Default)?.Lifetime;
        }

        public event InjectorEventHandler<InjectorEventArg> OnServiceRequest;

        public event InjectorEventHandler<InjectorEventArg> OnServiceRequested;
        #endregion

        #region Composite
        public override IServiceContainer CreateChild() => throw new NotSupportedException();

        public override void AddChild(IServiceContainer child) => throw new NotSupportedException();

        public override void RemoveChild(IServiceContainer child) => throw new NotSupportedException();
        #endregion
    }
}