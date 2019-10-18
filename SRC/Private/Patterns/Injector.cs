/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class Injector : ServiceContainer, IInjector
    {
        private readonly Stack<Type> FCurrentPath = new Stack<Type>();

        private Injector() => throw new NotSupportedException();

        public Injector(IServiceContainer parent): base(parent)
        {
            //
            // Felvesszuk sajat magunkat.
            //

            Add(new InstanceServiceEntry
            (
                @interface: typeof(IInjector),
                value: this,
                releaseOnDispose: false,
                owner: this
            ));
        }

        #region IInjector
        public object Get(Type iface, Type target)
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

            var ev = new InjectorEventArg(iface, target);
            OnServiceRequest?.Invoke(this, ev);
            if (ev.Service != null) return ev.Service;

            FCurrentPath.Push(iface);
            try
            {
                //
                // Ha egynel tobbszor szerepel az aktualis interface akkor korkoros referenciank van.
                //

                if (FCurrentPath.Count(t => t == iface) > 1)
                    throw new InvalidOperationException(string.Format(Resources.CIRCULAR_REFERENCE, string.Join(" -> ", FCurrentPath)));

                IServiceFactory factory = Get(iface, QueryMode.AllowSpecialization | QueryMode.ThrowOnError);

                //
                // Ha az OnServiceRequested esemenyben felulirjak a szervizt akkor azt
                // adjuk vissza.
                //

                ev.Service = factory.GetService(this, iface);
                OnServiceRequested?.Invoke(this, ev);

                return ev.Service;
            }
            finally
            {
                Type removed = FCurrentPath.Pop();
                Debug.Assert(removed == iface);
            }
        }

        public Lifetime? LifetimeOf(Type iface)
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

            return Get(iface, QueryMode.Default)?.Lifetime;
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