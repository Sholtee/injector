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

        public override void GetService(IInjector injector, ref ServiceReference reference)
        {
            try
            {
                CheckProducible();

                //
                // Ha mar kroabban le lett gyartva akkor visszaadjuk azt.
                //

                if (FService != null) reference = FService;
                else
                {
                    reference.Instance = Factory(injector, Interface); // elso helyen szerepeljen h a "finally" jol mukodjon
                    FService = reference;
                }
            }
            finally
            {
                if (FService != reference) reference.Dispose();
            }
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            CheckDisposed();

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