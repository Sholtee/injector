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
    public abstract class ProducibleServiceEntry : ServiceEntry
    {
        protected object FImplementation;

        protected ProducibleServiceEntry(Type @interface, Lifetime? lifetime, Func<IInjector, Type, object> factory) : base(@interface, lifetime)
        {
            Factory = factory;
        }

        protected ProducibleServiceEntry(Type @interface, Lifetime? lifetime, Type implementation) : this(@interface, lifetime, !@interface.IsGenericTypeDefinition() ? Resolver.Get(implementation).ConvertToFactory() : null)
        {
            FImplementation = implementation;
        }

        protected ProducibleServiceEntry(Type @interface, Lifetime? lifetime, ITypeResolver implementation): this(@interface, lifetime, !@interface.IsGenericTypeDefinition() ? Resolver.Get(implementation, @interface).ConvertToFactory() : null)
        {
            //
            // A "LazyThreadSafetyMode.ExecutionAndPublication" miatt orokolt bejegyzesek eseten is
            // csak egyszer fog meghivasra kerulni a resolver (adott interface-el).
            //

            FImplementation = new Lazy<Type>(() => implementation.Resolve(@interface), LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public sealed override Type Implementation => (FImplementation as Lazy<Type>)?.Value ?? (Type) FImplementation;
        public sealed override Func<IInjector, Type, object> Factory { get; set; }
        public override bool IsService => FImplementation != null;
        public override bool IsLazy => FImplementation is Lazy<Type>;
        public override bool IsFactory => !IsService && Factory != null;
        public override bool IsInstance => false;
    }
}