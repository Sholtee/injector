/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class InstanceServiceEntry : AbstractServiceEntry
    {
        private readonly IReadOnlyCollection<ServiceReference> FInstances;
        private IServiceRegistry? FRegistry; 

        public InstanceServiceEntry(Type @interface, string? name, object instance, bool externallyOwned, IServiceRegistry? owner) : base(@interface, name, null, owner)
        {
            Ensure.Parameter.IsNotNull(instance, nameof(instance));

            FInstances = new[] 
            { 
                new ServiceReference(this, instance, externallyOwned)
            };

            //
            // Ez kivetelt fog dobni ha "@interface"-en akar csak egy aspektus is van (peldanynak nincs
            // factory-ja -> nem lehet proxy-zni).
            //

            this.ApplyAspects();

            //
            // SetInstance() ne legyen hivva.
            //

            State = ServiceEntryStates.Built;
        }

        public override Lifetime? Lifetime { get; } = Lifetime.Instance;

        public override IServiceRegistry? Owner => FRegistry;

        public override AbstractServiceEntry CopyTo(IServiceRegistry owner)
        {
            FRegistry = owner;
            return this;
        }

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;

        public override bool SetInstance(IServiceReference reference) => throw new NotImplementedException();

        public override bool IsShared { get; } = true;
    }
}