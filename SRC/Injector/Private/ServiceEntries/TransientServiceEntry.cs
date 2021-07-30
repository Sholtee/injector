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

    internal class TransientServiceEntry : ProducibleServiceEntry
    {
        private readonly List<IServiceReference> FInstances = new();

        private TransientServiceEntry(TransientServiceEntry entry, IServiceContainer owner) : base(entry, owner) 
        {
            MaxSpawnedServices = entry.MaxSpawnedServices;
        }

        protected override void SaveReference(IServiceReference serviceReference) => FInstances.Add(serviceReference);

        public TransientServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, int maxSpawnedServices) : base(@interface, name, factory, owner)
        {
            MaxSpawnedServices = maxSpawnedServices;
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner, int maxSpawnedServices) : base(@interface, name, implementation, owner)
        {
            MaxSpawnedServices = maxSpawnedServices;
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, int maxSpawnedServices) : base(@interface, name, implementation, explicitArgs, owner)
        {
            MaxSpawnedServices = maxSpawnedServices;
        }

        public int MaxSpawnedServices { get; }

        public override bool SetInstance(IServiceReference reference)
        {
            CheckNotDisposed();
            EnsureAppropriateReference(reference);

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

            if (Instances.Count >= MaxSpawnedServices)
                //
                // Ha ide jutunk az azt jelenti h jo esellyel a tartalmazo injector ujrahasznositasra kerult
                // (ahogy az a teljesitmeny teszteknel meg is tortent).
                //

                #pragma warning disable CA2201 // Do not change the exception type to preserve backward compatibility
                throw new Exception(string.Format(Resources.Culture, Resources.INJECTOR_SHOULD_BE_RELEASED, MaxSpawnedServices));
                #pragma warning restore CA2201

            return base.SetInstance(reference);
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(target, nameof(target));
            
            var result = new TransientServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        public override Lifetime Lifetime { get; } = Lifetime.Transient;

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;
    }
}