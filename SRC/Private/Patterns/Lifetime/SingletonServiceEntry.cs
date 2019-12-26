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
    internal class SingletonServiceEntry : ProducibleServiceEntry
    {
        private ServiceReference FService;

        //
        // Lock azert kell mert ez az entitas egyszerre tobb szerviz kollekcioban is szerepelhet
        // (GetService() lehet hivva parhuzamosan).
        //

        private readonly object FLock = new object();

        public SingletonServiceEntry(Type @interface, string name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Singleton, factory, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string name, Type implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Singleton, implementation, owner)
        {
        }

        public SingletonServiceEntry(Type @interface, string name, ITypeResolver implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Singleton, implementation, owner)
        {
        }

        public override object Value => FService?.Instance;

        public override void GetService(IInjector injector, ref ServiceReference reference)
        {
            try
            {
                CheckProducible();

                if (FService == null)
                {
                    lock (FLock)
                    {
                        if (FService == null)
                        {
                            reference.Instance = Factory(injector, Interface); // elso helyen szerepeljen h a "finally" jol mukodjon
                            FService = reference;

                            return;
                        }
                    }
                }

                reference = FService;
            }
            finally
            {
                if (reference != FService) reference.Dispose();
            }
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) FService?.Release();

            base.Dispose(disposeManaged);
        }
    }
}