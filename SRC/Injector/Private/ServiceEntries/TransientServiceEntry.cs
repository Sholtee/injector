/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    /// <summary>
    /// Describes a transient service entry.
    /// </summary>
    internal class TransientServiceEntry : ProducibleServiceEntry
    {
        private readonly List<IServiceReference> FInstances = new();

        private TransientServiceEntry(TransientServiceEntry entry, IServiceContainer owner) : base(entry, owner) 
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        public override bool SetInstance(IServiceReference reference)
        {
            EnsureAppropriateReference(reference);
            EnsureProducible();
            
            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

            int? threshold = relatedInjector
                .Get<IReadOnlyDictionary<string, object>>($"{ServiceContainer.INTERNAL_SERVICE_NAME_PREFIX}options")
                .GetValueOrDefault<int?>("MaxSpawnedTransientServices");

            if (FInstances.Count >= threshold)
                //
                // Ha ide jutunk az azt jelenti h jo esellyel a tartalmazo injector ujrahasznositasra kerult
                // (ahogy az a teljesitmeny teszteknel meg is tortent).
                //

                #pragma warning disable CA2201 // Do not change the exception type to preserve backward compatibility
                throw new Exception(string.Format(Resources.Culture, Resources.INJECTOR_SHOULD_BE_RELEASED, threshold));
                #pragma warning restore CA2201

                //
                // "Factory" biztos nem NULL [lasd EnsureProducible()]
                //

            reference.Value = Factory!(relatedInjector, Interface);

            FInstances.Add(reference);
            State |= ServiceEntryStates.Instantiated;

            return true;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            Ensure.Parameter.IsNotNull(target, nameof(target));
            Ensure.NotDisposed(this);

            var result = new TransientServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;

        public override Lifetime Lifetime { get; } = Lifetime.Transient;
    }
}