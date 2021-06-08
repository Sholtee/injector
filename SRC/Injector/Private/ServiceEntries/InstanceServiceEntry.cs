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
    using Properties;

    internal class InstanceServiceEntry : AbstractServiceEntry
    {
        private readonly IReadOnlyCollection<ServiceReference> FInstances;

        public InstanceServiceEntry(Type @interface, string? name, object instance, bool externallyOwned, IServiceContainer owner) : base(
            @interface, 
            name, 
            owner)
        {
            Ensure.Parameter.IsNotNull(instance, nameof(instance));

            if (!Interface.IsInstanceOfType(instance))
                throw new InvalidCastException(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, Interface));

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

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;

        public override bool SetInstance(IServiceReference reference) =>
            //
            // Peldany eseten ez a metodus elvileg sose kerulhet meghivasra.
            //

            throw new NotImplementedException();

        public override bool IsShared => true;
    }
}