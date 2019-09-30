/********************************************************************************
* ScopedServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes a scoped service entry.
    /// </summary>
    internal class ScopedServiceEntry : ProducibleServiceEntry
    {
        private object FValue;

        private ScopedServiceEntry(ScopedServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, DI.Lifetime.Scoped, factory, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, Type implementation, IServiceContainer owner) : base(@interface, DI.Lifetime.Scoped, implementation, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, ITypeResolver implementation, IServiceContainer owner) : base(@interface, DI.Lifetime.Scoped, implementation, owner)
        {
        }

        public override object Value => FValue;

        public override object GetService(IInjector injector, Type iface = null)
        {
            CheckProducible();

            if (iface != null && iface != Interface)
                throw new NotSupportedException(Resources.NOT_SUPPORTED);

            return FValue ?? (FValue = Factory(injector, Interface));
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            var result = new ScopedServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                (FValue as IDisposable)?.Dispose();
                FValue = null;
            }
            base.Dispose(disposeManaged);
        }
    }
}