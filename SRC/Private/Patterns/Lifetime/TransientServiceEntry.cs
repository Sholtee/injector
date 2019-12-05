/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a transient service entry.
    /// </summary>
    internal class TransientServiceEntry : ProducibleServiceEntry
    {
        private readonly List<ServiceReference> FServices = new List<ServiceReference>();

        private TransientServiceEntry(TransientServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Transient, factory, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string name, Type implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Transient, implementation, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string name, ITypeResolver implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Transient, implementation, owner)
        {
        }

        public override object Value => null;

        public override ServiceReference GetService(Func<IInjector> injectorFactory, ServiceReference reference)
        {
            CheckProducible();

            reference.Instance = Factory(injectorFactory(), Interface);
            FServices.Add(reference);

            return reference;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            var result = new TransientServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) 
            {
                FServices.ForEach(svc => svc.Release()); 
                FServices.Clear();
            }

            base.Dispose(disposeManaged);
        }
    }
}