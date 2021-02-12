/********************************************************************************
* ProducibleServiceEntryBase.cs                                                 *
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

    internal abstract class ProducibleServiceEntryBase : AbstractServiceEntry
    {
        #region Protected
        protected ProducibleServiceEntryBase(ProducibleServiceEntryBase entry, IServiceContainer owner) : base(entry.Interface, entry.Name, entry.Implementation, owner, entry.CustomConverters.ToArray())
        {
            Factory = entry.Factory;
            ExplicitArgs = entry.ExplicitArgs;

            //
            // Itt nem kell "this.ApplyAspects()" hivas mert a forras bejegyzesen mar
            // hivva volt.
            //
        }

        protected ProducibleServiceEntryBase(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, null, owner, customConverters)
        {
            //
            // Os ellenorzi az interface-t es a tulajdonost.
            //

            Factory = Ensure.Parameter.IsNotNull(factory, nameof(factory));
            this.ApplyAspects();
        }

        protected ProducibleServiceEntryBase(Type @interface, string? name, Type implementation, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, owner, customConverters)
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

        protected ProducibleServiceEntryBase(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, owner, customConverters)
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
    }
}