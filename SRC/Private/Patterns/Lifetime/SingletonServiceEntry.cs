/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a singleton service entry.
    /// </summary>
    internal class SingletonServiceEntry : ProducibleServiceEntry
    {
        private object FValue;

        public SingletonServiceEntry(Type @interface, Func<IInjector, Type, object> factory, ICollection<ServiceEntry> owner) : base(@interface, DI.Lifetime.Singleton, factory, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, Type implementation, ICollection<ServiceEntry> owner) : base(@interface, DI.Lifetime.Singleton, implementation, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, ITypeResolver implementation, ICollection<ServiceEntry> owner) : base(@interface, DI.Lifetime.Singleton, implementation, owner)
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

        public override object Clone() => new SingletonServiceEntry(Interface, Factory, Owner)
        {
            //
            // Ne az Implementation-t magat adjuk at h a resolver ne triggerelodjon ha 
            // meg nem volt hivva.
            //

            FImplementation = FImplementation
        };

        public override ServiceEntry CopyTo(ICollection<ServiceEntry> target)
        {
            var result = new SingletonServiceEntry(Interface, Factory, target)
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