/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    /// <summary>
    /// Describes a producible service entry.
    /// </summary>
    internal abstract class ProducibleServiceEntry : AbstractServiceEntry, ISupportsProxying, ISupportsSpecialization
    {
        #region Protected
        protected ProducibleServiceEntry(ProducibleServiceEntry entry, IServiceContainer owner) : base(entry.Interface, entry.Name, entry.Implementation, owner)
        {
            Factory = entry.Factory;
            ExplicitArgs = entry.ExplicitArgs;

            //
            // Itt nem kell "this.ApplyAspects()" hivas mert a forras bejegyzesen mar
            // hivva volt.
            //
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, null, owner)
        {
            //
            // Os ellenorzi az interface-t es a tulajdonost.
            //

            Factory = Ensure.Parameter.IsNotNull(factory, nameof(factory));
            this.ApplyAspects();
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
            //
            // Os ellenorzi a tobbit.
            //

            Ensure.Parameter.IsNotNull(implementation, nameof(implementation));

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

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
            //
            // Os ellenorzi a tobbit.
            //

            Ensure.Parameter.IsNotNull(implementation, nameof(implementation));
            Ensure.Parameter.IsNotNull(explicitArgs, nameof(explicitArgs));

            if (!@interface.IsGenericTypeDefinition)
            {
                Func<IInjector, IReadOnlyDictionary<string, object?>, object> factoryEx = Resolver.GetExtended(implementation);

                Factory = (injector, _) => factoryEx(injector, explicitArgs!);
                this.ApplyAspects();
            }
            else
                implementation.GetApplicableConstructor();

            ExplicitArgs = explicitArgs;
        }

        protected void EnsureAppropriateReference(IServiceReference reference)
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
        #endregion

        public IReadOnlyDictionary<string, object?>? ExplicitArgs { get; }

        #region Features
        Func<IInjector, Type, object>? ISupportsProxying.Factory { get => Factory; set => Factory = value; }

        AbstractServiceEntry ISupportsSpecialization.Specialize(params Type[] genericArguments) 
        {
            Type closedIface = Interface.MakeGenericType(genericArguments);

            //
            // "Service(typeof(IGeneric<>), ...)" eseten az implementaciot konkretizaljuk.
            //

            if (Implementation is not null)
            {
                Type closedImpl = Implementation.MakeGenericType(genericArguments);

                return ExplicitArgs is not null
                    ? Lifetime!.CreateFrom(closedIface, Name, closedImpl, ExplicitArgs, Owner).Single()
                    : Lifetime!.CreateFrom(closedIface, Name, closedImpl, Owner).Single();
            }

            //
            // "Factory(typeof(IGeneric<>), ...)" eseten az eredeti factory lesz hivva a 
            // konkretizalt interface-re.
            //

            if (Factory is not null) 
                return Lifetime!.CreateFrom(closedIface, Name, Factory, Owner).Single();

            throw new NotSupportedException();
        }
        #endregion
    }
}