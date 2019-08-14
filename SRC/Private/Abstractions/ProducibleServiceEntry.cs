/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a producible service entry.
    /// </summary>
    internal abstract class ProducibleServiceEntry : ServiceEntry
    {
        protected object FImplementation;

        protected ProducibleServiceEntry(Type @interface, Lifetime lifetime, Func<IInjector, Type, object> factory) : base(@interface, lifetime)
        {
            Factory = factory;
        }

        protected ProducibleServiceEntry(Type @interface, Lifetime lifetime, Type implementation) : this(@interface, lifetime, !@interface.IsGenericTypeDefinition() ? Resolver.Get(implementation).ConvertToFactory() : null)
        {
            FImplementation = implementation;
        }

        protected ProducibleServiceEntry(Type @interface, Lifetime lifetime, ITypeResolver implementation): base(@interface, lifetime)
        {
            var lazyImplementation = new Lazy<Type>
            (
                () => implementation.Resolve(@interface),

                //
                // A "LazyThreadSafetyMode.ExecutionAndPublication" miatt orokolt bejegyzesek eseten is
                // csak egyszer fog meghivasra kerulni a resolver (adott interface-el).
                //

                LazyThreadSafetyMode.ExecutionAndPublication
            );

            FImplementation = lazyImplementation;

            //
            // Mivel van factory ezert lusta bejegyzesek is Proxy-zhatok.
            //

            if (!@interface.IsGenericTypeDefinition())
                Factory = Resolver.Get(lazyImplementation).ConvertToFactory();
        }

        protected void CheckProducible()
        {
            CheckDisposed();

            //
            // Ha nincs factory akkor amugy sem lehet peldanyositani a szervizt tok mind1 mi az.
            //

            if (Factory == null)
                throw new InvalidOperationException();
        }

        protected void CheckInterfaceSupported(Type iface)
        {
            //
            // Generikust sose peldanyosithatunk.
            //

            if (iface.IsGenericTypeDefinition())
                throw new ArgumentException("TODO", nameof(iface));

            //
            // Generikus interface-hez tartozo factory-nal megengedjuk specializalt peldany lekerdezeset.
            //

            if (IsFactory && Interface.IsGenericTypeDefinition())
            {
                if (!iface.IsGenericType())
                    throw new ArgumentException("TODO", nameof(iface));

                iface = iface.GetGenericTypeDefinition();
            }

            //
            // Minden mas esetben csak a regisztralt szervizt kerdezhetjuk le.
            //

            if (iface != Interface)
                throw new ArgumentException("TODO", nameof(iface));
        }

        public sealed override Type Implementation => (FImplementation as Lazy<Type>)?.Value ?? (Type) FImplementation;
        public sealed override Func<IInjector, Type, object> Factory { get; set; }
        public override bool IsService => FImplementation != null;
        public override bool IsLazy => FImplementation is Lazy<Type>;
        public override bool IsFactory => !IsService && Factory != null;
        public override bool IsInstance => false;
    }
}