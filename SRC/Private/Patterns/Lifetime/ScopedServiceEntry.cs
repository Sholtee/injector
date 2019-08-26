/********************************************************************************
* ScopedServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a scoped service entry.
    /// </summary>
    internal class ScopedServiceEntry : ProducibleServiceEntry
    {
        private object FValue;

        public ScopedServiceEntry(Type @interface, Func<IInjector, Type, object> factory, ICollection<ServiceEntry> owner) : base(@interface, DI.Lifetime.Scoped, factory, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, Type implementation, ICollection<ServiceEntry> owner) : base(@interface, DI.Lifetime.Scoped, implementation, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, ITypeResolver implementation, ICollection<ServiceEntry> owner) : base(@interface, DI.Lifetime.Scoped, implementation, owner)
        {
        }

        public override object Value => FValue;

        public override object GetService(IInjector injector, Type iface = null)
        {
            CheckProducible();

            if (iface != null)
                CheckInterfaceSupported(iface);

            return FValue ?? (FValue = Factory(injector, iface ?? Interface));
        }

        public override object Clone() => new ScopedServiceEntry(Interface, Factory, Owner)
        {
            //
            // Ne az Implementation-t magat adjuk at h a resolver ne triggerelodjon ha 
            // meg nem volt hivva.
            //

            FImplementation = FImplementation
        };

        public override ServiceEntry CopyTo(ICollection<ServiceEntry> target)
        {
            var result = new ScopedServiceEntry(Interface, Factory, target)
            {
                FImplementation = FImplementation
            };

            target?.Add(result);
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