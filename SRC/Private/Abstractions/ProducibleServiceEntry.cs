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
    internal abstract partial class ProducibleServiceEntry : AbstractServiceEntry
    {
        internal object UnderlyingImplementation { get; }

        protected ProducibleServiceEntry(ProducibleServiceEntry entry, IServiceContainer owner): base(entry.Interface, entry.Name, entry.Lifetime, owner)
        {
            Factory = entry.Factory;

            //
            // Ne az Implementation-t magat adjuk at h a resolver ne triggerelodjon ha 
            // meg nem volt hivva.
            //

            UnderlyingImplementation = entry.UnderlyingImplementation;
        }

        protected ProducibleServiceEntry(Type @interface, string name, Lifetime lifetime, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, lifetime, owner)
        {
            //
            // Interface-t nem kell ellenorizni (az os megteszi).
            //

            Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        protected ProducibleServiceEntry(Type @interface, string name, Lifetime lifetime, Type implementation, IServiceContainer owner) : base(@interface, name, lifetime, owner)
        {
            //
            // Interface-t nem kell ellenorizni (az os megteszi).
            //

            if (implementation == null)
                throw new ArgumentNullException(nameof(implementation));

            //
            // Implementacio validalas (mukodik generikusokra is).
            //

            if (!@interface.IsInterfaceOf(implementation))
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.NOT_ASSIGNABLE, @interface, implementation));

            if (!@interface.IsGenericTypeDefinition())
                Factory = Resolver.Get(implementation);
            else
                //
                // Konstruktor validalas csak generikus esetben kell (mert ilyenkor nincs Resolver.Get()
                // hivas). A GetApplicableConstructor() validal valamint mukodik generikusokra is.
                // 

                implementation.GetApplicableConstructor();

            UnderlyingImplementation = implementation;
        }

        protected ProducibleServiceEntry(Type @interface, string name, Lifetime lifetime, ITypeResolver implementation, IServiceContainer owner) : base(@interface, name, lifetime, owner)
        {
            if (implementation == null)
                throw new ArgumentNullException(nameof(implementation));

            //
            // A konstruktort validalni fogja a Resolver.Get() hivas az elso peldanyositaskor, igy
            // itt nekunk csak azt kell ellenoriznunk, h az interface tamogatott e.
            //

            if (!implementation.Supports(@interface))
                throw new NotSupportedException(string.Format(Resources.Culture, Resources.INTERFACE_NOT_SUPPORTED, @interface));

            Lazy<Type> lazyImplementation = implementation.AsLazy(@interface);

            if (!@interface.IsGenericTypeDefinition())
                //
                // Mivel van factory ezert lusta bejegyzesek is Proxy-zhatok.
                //

                Factory = Resolver.Get(lazyImplementation);

            UnderlyingImplementation = lazyImplementation;
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
        public sealed override Func<IInjector, Type, object> Factory { get; set; }
    }
}