/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal abstract class ProducibleServiceEntry : AbstractServiceEntry, ISupportsSpecialization, ISupportsProxying
    {
        #region Protected
        protected ProducibleServiceEntry(ProducibleServiceEntry entry, IServiceRegistry? owner) : base(entry.Interface, entry.Name, entry.Implementation, owner)
        {
            Factory = entry.Factory;
            ExplicitArgs = entry.ExplicitArgs;
            Root = entry.Root ?? entry;

            //
            // Ha korabban mar sikerult validalni a bejegyzest [lasd UpdateState()] akkor azt a masolaton mar nem
            // kell megtenni.
            //
            // Megjegyzes:
            //    Ez csak regularis bejegyzesekre mukodik mivel azoknak van szuloje.
            //

            if (Root.Flags.HasFlag(ServiceEntryFlags.Validated))
                Flags = ServiceEntryFlags.Validated;

            //
            // Itt nem kell "this.ApplyAspects()" hivas mert a forras bejegyzesen mar
            // hivva volt.
            //
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceRegistry? owner) : base(@interface, name, null, owner)
        {
            //
            // Os ellenorzi az interface-t es a tulajdonost.
            //

            Factory = Ensure.Parameter.IsNotNull(factory, nameof(factory));
            this.ApplyAspects();
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, IServiceRegistry? owner) : base(@interface, name, implementation, owner)
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

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, IServiceRegistry? owner) : base(@interface, name, implementation, owner)
        {
            //
            // Os ellenorzi a tobbit.
            //

            Ensure.Parameter.IsNotNull(implementation, nameof(implementation));
            Ensure.Parameter.IsNotNull(explicitArgs, nameof(explicitArgs));

            if (!@interface.IsGenericTypeDefinition)
            {
                if (explicitArgs is IReadOnlyDictionary<string, object?> dict)
                {
                    Func<IInjector, IReadOnlyDictionary<string, object?>, object> factoryEx = ServiceActivator.GetExtended(implementation);

                    Factory = (injector, _) => factoryEx(injector, dict);
                }
                else
                {
                    Func<IInjector, object, object> factoryEx = ServiceActivator.GetExtended(implementation, explicitArgs.GetType());

                    Factory = (injector, _) => factoryEx(injector, explicitArgs);
                }

                this.ApplyAspects();
            }
            else
                //
                // Validalas vegett
                //

                implementation.GetApplicableConstructor();

            ExplicitArgs = explicitArgs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EnsureProducible()
        {
            if (Factory is null)
                throw new InvalidOperationException(Resources.NOT_PRODUCIBLE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void UpdateState(ServiceEntryFlags newState) // TODO: remove
        {
            if (newState == Flags)
                return;

            Debug.Assert(newState > Flags, "New state must be greater than the old one.");

            Flags = newState;

            if (Flags.HasFlag(ServiceEntryFlags.Validated) && Root?.Flags < ServiceEntryFlags.Validated)
                //
                // Nem gond ha ez parhuzamosan kerul meghivasra tobb kulonbozo masolt bejegyzesbol (az ertekadas
                // atomi muvelet es a validalt allapoton kivul mas nem kerul beallitasra a gyokerben).
                //

                Root.Flags = ServiceEntryFlags.Validated;
        }
        #endregion

        public override object CreateInstance(IInjector scope, out IDisposable? lifetime)
        {
            EnsureProducible();

            object result = Factory!(scope, Interface);
            lifetime = result as IDisposable;
            return result;
        }

        public ProducibleServiceEntry? Root { get; } // TODO: remove

        public object? ExplicitArgs { get; }

        public abstract AbstractServiceEntry Specialize(IServiceRegistry? owner /*TODO: remove*/, params Type[] genericArguments);

        Func<IInjector, Type, object>? ISupportsProxying.Factory
        {
            get => Factory;
            set => Factory = value;
        }
    }
}