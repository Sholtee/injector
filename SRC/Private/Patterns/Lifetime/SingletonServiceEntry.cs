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

        public SingletonServiceEntry(Type @interface, Func<IInjector, Type, object> factory) : base(@interface, DI.Lifetime.Singleton, factory)
        {
        }

        public SingletonServiceEntry(Type @interface, Type implementation) : base(@interface, DI.Lifetime.Singleton, implementation)
        {
        }

        public SingletonServiceEntry(Type @interface, ITypeResolver implementation) : base(@interface, DI.Lifetime.Singleton, implementation)
        {
        }

        public override object Value => FValue;

        public override object GetService(IInjector injector) => FValue ?? (FValue = Factory(injector, Interface));

        public override object Clone() => new SingletonServiceEntry(Interface, Factory)
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