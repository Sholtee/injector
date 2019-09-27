/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes a producible service entry.
    /// </summary>
    internal abstract class ProducibleServiceEntry : AbstractServiceEntry
    {
        protected ProducibleServiceEntry(ProducibleServiceEntry entry, IServiceContainer owner): base(entry.Interface, entry.Lifetime, owner)
        {
            Factory = entry.Factory;

            //
            // Ne az Implementation-t magat adjuk at h a resolver ne triggerelodjon ha 
            // meg nem volt hivva.
            //

            UnderlyingImplementation = entry.UnderlyingImplementation;
        }

        protected ProducibleServiceEntry(Type @interface, Lifetime lifetime, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, lifetime, owner)
        {
            Factory = factory;
        }

        protected ProducibleServiceEntry(Type @interface, Lifetime lifetime, Type implementation, IServiceContainer owner) : this(@interface, lifetime, !@interface.IsGenericTypeDefinition() ? Resolver.Get(implementation).ConvertToFactory() : null, owner)
        {
            UnderlyingImplementation = implementation;
        }

        protected ProducibleServiceEntry(Type @interface, Lifetime lifetime, ITypeResolver implementation, IServiceContainer owner) : base(@interface, lifetime, owner)
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

            UnderlyingImplementation = lazyImplementation;

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
                throw new InvalidOperationException(Resources.NOT_PRODUCIBLE);
        }

        public sealed override Type Implementation => (UnderlyingImplementation as Lazy<Type>)?.Value ?? (Type) UnderlyingImplementation;
        public override object UnderlyingImplementation { get; }
        public sealed override Func<IInjector, Type, object> Factory { get; set; }
    }
}