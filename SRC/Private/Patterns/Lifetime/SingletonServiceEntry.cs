/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a singleton service entry.
    /// </summary>
    public class SingletonServiceEntry : ProducibleServiceEntry
    {
        private object FValue;

        public SingletonServiceEntry(Type @interface, Lifetime? lifetime, Func<IInjector, Type, object> factory) : base(@interface, lifetime, factory)
        {
        }

        public SingletonServiceEntry(Type @interface, Lifetime? lifetime, Type implementation) : base(@interface, lifetime, implementation)
        {
        }

        public SingletonServiceEntry(Type @interface, Lifetime? lifetime, ITypeResolver implementation) : base(@interface, lifetime, implementation)
        {
        }

        public override object Value => FValue;

        public override object GetService(IInjector injector) => FValue ?? (FValue = Factory(injector, Interface));

        public override object Clone() => new SingletonServiceEntry(Interface, Lifetime, Factory)
        {
            FImplementation = FImplementation
        };

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) (FValue as IDisposable)?.Dispose();
            base.Dispose(disposeManaged);
        }
    }
}