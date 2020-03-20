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
    internal abstract partial class ProducibleServiceEntry : AbstractServiceEntry, ISupportsProxying, ISupportsSpecialization, IHasUnderlyingImplementation
    {
        internal ProducibleServiceEntry(Type @interface, string? name, Lifetime? lifetime, IServiceContainer owner) : base(
            @interface, 
            name, 
            Ensure.Parameter.IsNotNull(lifetime, nameof(lifetime)), 
            Ensure.Parameter.IsNotNull(owner, nameof(owner))) 
        {
            //
            // Interface-t nem kell ellenorizni (az os megteszi).
            //
        }

        protected ProducibleServiceEntry(ProducibleServiceEntry entry, IServiceContainer owner): this(entry.Interface, entry.Name, entry.Lifetime, owner)
        {
            Factory = entry.Factory;

            //
            // Ne az Implementation-t magat adjuk at h a resolver ne triggerelodjon ha 
            // meg nem volt hivva.
            //

            UnderlyingImplementation = entry.UnderlyingImplementation;
        }

        protected ProducibleServiceEntry(Type @interface, string name, Lifetime lifetime, Func<IInjector, Type, object> factory, IServiceContainer owner) : this(@interface, name, lifetime, owner) =>
            Factory = Ensure.Parameter.IsNotNull(factory, nameof(factory));

        protected ProducibleServiceEntry(Type @interface, string name, Lifetime lifetime, Type implementation, IServiceContainer owner) : this(@interface, name, lifetime, owner)
        {
            //
            // Interface-t nem kell ellenorizni (az os megteszi).
            //

            Ensure.Parameter.IsNotNull(implementation, nameof(implementation));
            Ensure.Type.Supports(implementation, @interface);

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

        protected ProducibleServiceEntry(Type @interface, string name, Lifetime lifetime, ITypeResolver implementation, IServiceContainer owner) : this(@interface, name, lifetime, owner)
        {
            //
            // Interface-t nem kell ellenorizni (az os megteszi).
            //

            Ensure.Parameter.IsNotNull(implementation, nameof(implementation));

            //
            // - A tenyleges implementacio az elso hivatkozaskor lesz validalva (lasd AsLazy() implementacio).
            // - A konstruktor pedig az elso peldanyositaskor (lasd Resolver.Get() implementacio).
            //

            Ensure.Type.Supports(implementation, @interface);

            Lazy<Type> lazyImplementation = implementation.AsLazy(@interface);

            if (!@interface.IsGenericTypeDefinition())
                //
                // Mivel van factory ezert lusta bejegyzesek is Proxy-zhatok.
                //

                Factory = Resolver.Get(lazyImplementation);

            UnderlyingImplementation = lazyImplementation;
        }

        protected void EnsureEmptyReference(ServiceReference reference)
        {
            Ensure.Parameter.IsNotNull(reference, nameof(reference));
            Ensure.AreEqual(reference.RelatedServiceEntry, this, Resources.NOT_BELONGING_REFERENCE);
            Ensure.IsNull(reference.Value, $"{nameof(reference)}.{nameof(reference.Value)}");
        }

        protected void EnsureProducible()
        {
            Ensure.NotDisposed(this);

            //
            // Ha nincs factory akkor amugy sem lehet peldanyositani a szervizt tok mind1 mi az.
            //

            if (Factory == null)
                throw new InvalidOperationException(Resources.NOT_PRODUCIBLE);
        }

        public object? UnderlyingImplementation { get; }

        public override Type? Implementation => (UnderlyingImplementation as Lazy<Type>)?.Value ??  (Type?) UnderlyingImplementation;

        Func<IInjector, Type, object>? ISupportsProxying.Factory { get => Factory; set => Factory = value; }

        AbstractServiceEntry ISupportsSpecialization.Specialize(params Type[] genericArguments) 
        {
            //
            // "Service(typeof(IGeneric<>), ...)" eseten az implementaciot konkretizaljuk.
            //

            if (Implementation != null) return SpecializeBy
            (
                Implementation.MakeGenericType(genericArguments)
            );

            //
            // "Factory(typeof(IGeneric<>), ...)" eseten az eredeti factory lesz hivva a 
            // konkretizalt interface-re.
            //

            if (Factory != null) return SpecializeBy
            (
                Factory
            );

            throw new NotSupportedException();

            AbstractServiceEntry SpecializeBy<TParam>(TParam param) => ProducibleServiceEntry.Create
            (
                Lifetime,
                Interface.MakeGenericType(genericArguments),
                Name,
                param,
                
                //
                // Legyarthato entitasnak mindig kell legyen szuloje
                //

                Ensure.IsNotNull(Owner, nameof(Owner))
            );
        }
    }
}