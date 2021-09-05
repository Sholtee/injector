/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal abstract class ProducibleServiceEntry : AbstractServiceEntry, ISupportsSpecialization
    {
        #region Protected
        protected abstract void SaveReference(IServiceReference serviceReference);

        protected ProducibleServiceEntry(ProducibleServiceEntry entry, IServiceContainer owner) : base(entry.Interface, entry.Name, entry.Implementation, owner) // TODO: torolni
        {
            Factory = entry.Factory;
            ExplicitArgs = entry.ExplicitArgs;

            //
            // Itt nem kell "this.ApplyAspects()" hivas mert a forras bejegyzesen mar
            // hivva volt.
            //
        }

        protected ProducibleServiceEntry(ProducibleServiceEntry entry, IServiceRegistry owner) : base(entry.Interface, entry.Name, entry.Implementation, owner)
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
                Factory = ServiceActivator.Get(implementation);
                this.ApplyAspects();
            }
            else
                //
                // Konstruktor validalas csak generikus esetben kell (mert ilyenkor nincs Resolver.Get()
                // hivas). A GetApplicableConstructor() validal valamint mukodik generikusokra is.
                //
                // Generikus esetben az aspektusok a bejegyzes tipizalasakor lesznek alkalmazva.
                // 

                implementation.GetApplicableConstructor();
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
                Func<IInjector, IReadOnlyDictionary<string, object?>, object> factoryEx = ServiceActivator.GetExtended(implementation);

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
        #endregion

        public override bool SetInstance(IServiceReference serviceReference)
        {
            //
            // Ha nincs factory akkor amugy sem lehet peldanyositani a szervizt tok mind1 mi az.
            //

            if (Factory is null)
                throw new InvalidOperationException(Resources.NOT_PRODUCIBLE);

            serviceReference.Value = Factory(serviceReference.Scope!, Interface);
            SaveReference(serviceReference);

            State |= ServiceEntryStates.Instantiated;

            return true;
        }

        public abstract AbstractServiceEntry Specialize(params Type[] genericArguments);

        public abstract AbstractServiceEntry Specialize(IServiceRegistry owner, params Type[] genericArguments);

        public IReadOnlyDictionary<string, object?>? ExplicitArgs { get; }
    }
}