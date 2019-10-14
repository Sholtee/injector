/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

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

            UserData = entry.UserData;
        }

        protected ProducibleServiceEntry(Type @interface, Lifetime lifetime, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, lifetime, owner)
        {
            Factory = factory;
        }

        protected ProducibleServiceEntry(Type @interface, Lifetime lifetime, Type implementation, IServiceContainer owner) : this(@interface, lifetime, !@interface.IsGenericTypeDefinition() ? Resolver.Get(implementation) : null, owner)
        {
            UserData = implementation;
        }

        protected ProducibleServiceEntry(Type @interface, Lifetime lifetime, ITypeResolver implementation, IServiceContainer owner) : base(@interface, lifetime, owner)
        {
            Lazy<Type> lazyImplementation = implementation.AsLazy(@interface);

            UserData = lazyImplementation;

            if (!@interface.IsGenericTypeDefinition())
                //
                // Mivel van factory ezert lusta bejegyzesek is Proxy-zhatok.
                //

                Factory = Resolver.Get(lazyImplementation);
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

        public sealed override Type Implementation => (UserData as Lazy<Type>)?.Value ?? (Type) UserData;
        public sealed override object UserData { get; }
        public sealed override Func<IInjector, Type, object> Factory { get; set; }
    }
}