﻿/********************************************************************************
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
        #region Private
        private readonly Stack<Type> FCurrentPath = new Stack<Type>();

        private Injector() => throw new NotSupportedException();

        public Injector(IServiceContainer parent): base(parent)
        {
            //
            // Beallitjuk a proxyt, majd felvesszuk sajat magunkat.
            //

            Add(new InstanceServiceEntry
            (
                @interface: typeof(IInjector),
                value: this,
                releaseOnDispose: false,
                owner: this
            ));
        }
        #endregion

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

        public object Instantiate(Type @class, IReadOnlyDictionary<string, object> explicitArgs)
        {
            if (@class == null)
                throw new ArgumentNullException(nameof(@class));

            if (!@class.IsClass())
                throw new ArgumentException(Resources.NOT_A_CLASS, nameof(@class));

            if (@class.IsGenericTypeDefinition())
                throw new ArgumentException(Resources.CANT_INSTANTIATE_GENERICS, nameof(@class));

            //
            // Itt nincs ertelme OnServiceRequest esemenynek mivel nincs szerviz interface-unk.
            //

            Func<IInjector, IReadOnlyDictionary<string, object>, object> factory = Resolver.GetExtended(@class);

            //
            // Ha az OnServiceRequested esemenyben felulirjak a szervizt akkor azt
            // adjuk vissza.
            //

            var ev = new InjectorEventArg(@class)
            {
                Service = factory(this, explicitArgs ?? new Dictionary<string, object>(0))
            };
            OnServiceRequested?.Invoke(this, ev);

            return ev.Service;
        }

        public event InjectorEventHandler<InjectorEventArg> OnServiceRequest;

        public event InjectorEventHandler<InjectorEventArg> OnServiceRequested;
        #endregion
    }
}
