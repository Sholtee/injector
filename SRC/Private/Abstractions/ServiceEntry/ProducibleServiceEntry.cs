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
    internal abstract partial class ProducibleServiceEntry : AbstractServiceEntry, ISupportsProxying, ISupportsSpecialization
    {
        internal ProducibleServiceEntry(Type @interface, string? name, Lifetime lifetime, IServiceContainer owner) : base(
            @interface,
            name,
            lifetime,
            null,
            owner)
        {
            //
            // Os ellenorzi az interface-t es a tulajdonost.
            //
        }

        protected ProducibleServiceEntry(ProducibleServiceEntry entry, IServiceContainer owner) : base(entry.Interface, entry.Name, entry.Lifetime, entry.Implementation, owner)
        {
            Factory = entry.Factory;

            //
            // Itt nem kell "this.ApplyAspects()" hivas mert a forras bejegyzesen mar
            // hivva volt.
            //
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Lifetime lifetime, Func<IInjector, Type, object> factory, IServiceContainer owner) : this(@interface, name, lifetime, owner)
        {
            Factory = Ensure.Parameter.IsNotNull(factory, nameof(factory));
            this.ApplyAspects();
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Lifetime lifetime, Type implementation, IServiceContainer owner) : base(@interface, name, lifetime, implementation, owner)
        {
            //
            // Os ellenorzi az interface-t es a tulajdonost.
            //

            Ensure.Parameter.IsNotNull(implementation, nameof(implementation));
            Ensure.Type.Supports(implementation, @interface);

            if (!@interface.IsGenericTypeDefinition)
            {
                Factory = Resolver.Get(implementation);
                this.ApplyAspects();
            }
            else
                //
                // Konstruktor validalas csak generikus esetben kell (mert ilyenkor nincs Resolver.Get()
                // hivas). A GetApplicableConstructor() validal valamint mukodik generikusokra is.
                // 

                implementation.GetApplicableConstructor();

                //
                // Generikus esetben az aspektusok a bejegyzes tipizalasakor lesznek alkalmazva.
                //
        }

        protected void EnsureAppropriateReference(ServiceReference reference)
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

        #region Features
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
                Owner
            );
        }
        #endregion
    }
}