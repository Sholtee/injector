/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes a singleton service entry.
    /// </summary>
    internal class SingletonServiceEntry : ProducibleServiceEntry
    {
        private object FValue;

        //
        // Lock azert kell mert ez az entitas egyszerre tobb szerviz kollekcioban is szerepelhet
        // (GetService() lehet hivva parhuzamosan).
        //

        private readonly object FLock = new object();

        public SingletonServiceEntry(Type @interface, Func<IInjector, Type, object> factory, ServiceCollection owner) : base(@interface, DI.Lifetime.Singleton, factory, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, Type implementation, ServiceCollection owner) : base(@interface, DI.Lifetime.Singleton, implementation, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, ITypeResolver implementation, ServiceCollection owner) : base(@interface, DI.Lifetime.Singleton, implementation, owner)
        {
        }

        public override object Value => FValue;

        public override object GetService(IInjector injector, Type iface = null)
        {
            CheckProducible();

            if (iface != null && iface != Interface)
                throw new NotSupportedException(Resources.NOT_SUPPORTED);

            if (FValue == null)
                lock (FLock)
                    if (FValue == null)
                        FValue = Factory(injector, Interface);

            return FValue;
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