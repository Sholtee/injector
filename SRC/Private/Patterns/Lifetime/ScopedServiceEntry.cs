/********************************************************************************
* ScopedServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a scoped service entry.
    /// </summary>
    internal class ScopedServiceEntry : ProducibleServiceEntry
    {
        private ServiceReference FService;

        private ScopedServiceEntry(ScopedServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Scoped, factory, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string name, Type implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Scoped, implementation, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, string name, ITypeResolver implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Scoped, implementation, owner)
        {
        }

        public override object Value => FService?.Instance;

        public override ServiceReference GetService(Func<IInjector> injectorFactory, ServiceReference reference)
        {
            CheckProducible();

            if (FService == null) 
            {
                FService = reference;
                FService.Instance = Factory(injectorFactory(), Interface);
            }

            return FService;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            var result = new ScopedServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) FService?.Release();

            base.Dispose(disposeManaged);
        }
    }
}