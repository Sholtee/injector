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
        private readonly List<IServiceReference> FInstances = new List<IServiceReference>();

        private void EnsureNotFull(IReadOnlyDictionary<string, object> options) 
        {
            int? threshold = options?.GetValueOrDefault<int?>("MaxSpawnedTransientServices");

            if (FInstances.Count >= threshold)
                //
                // Ha ide jutunk az azt jelenti h jo esellyel a tartalmazo injector ujrahasznositasra kerult
                // (ahogy az a teljesitmeny teszteknel meg is tortent).
                //

                throw new Exception(string.Format(Resources.Culture, Resources.INJECTOR_SHOULD_BE_RELEASED, threshold));
        }

        private TransientServiceEntry(TransientServiceEntry entry, IServiceContainer owner) : base(entry, owner) 
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, factory, owner, customConverters)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, owner, customConverters)
        {
        }

        public TransientServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, explicitArgs, owner, customConverters)
        {
        }

        public override bool SetInstance(IServiceReference reference, IReadOnlyDictionary<string, object> options)
        {
            EnsureAppropriateReference(reference);
            EnsureProducible();
            EnsureNotFull(options);

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

            //
            // "Factory" biztos nem NULL [lasd EnsureProducible()]
            //

            reference.Value = Factory!(relatedInjector, Interface);
            FInstances.Add(reference);

            //
            // "Built" property erteket FALSE-on hagyjuk -> a SetInstance() legkozelebb is meghivasra kerul)
            //

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