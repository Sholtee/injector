/********************************************************************************
* SingletonServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

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
            CheckProducible();

            Debug.Assert(Owner.IsDescendantOf(injector.UnderlyingContainer.Parent));

            if (FService == null)
            {
                lock (FLock)
                {
                    if (FService == null)
                    {
                        //
                        // Ha meg nem lett legyartva akkor elkeszitjuk.
                        //

                        try
                        {
                            reference.Instance = Factory(injector, Interface);
                        }
                        catch 
                        {
                            reference.Release();
                            throw;
                        }

                        FService = reference;
                        return;
                    }
                }
            }

            //
            // Kulomben visszaadjuk a kroabban legyartott peldanyt.
            //

            reference.Release();
            reference = FService;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) FService?.Release();

            base.Dispose(disposeManaged);
        }
    }
}