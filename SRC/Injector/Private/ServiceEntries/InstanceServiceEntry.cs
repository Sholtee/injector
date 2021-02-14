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

    /// <summary>
    /// Describes an instance service entry.
    /// </summary>
    internal class InstanceServiceEntry : AbstractServiceEntry
    {
        private readonly IReadOnlyCollection<ServiceReference> FInstances;

        public InstanceServiceEntry(Type @interface, string? name, object instance, bool externallyOwned, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(
            @interface, 
            name, 
            owner,
            customConverters)
        {
            Ensure.Parameter.IsNotNull(instance, nameof(instance));

            //
            // Nem kell kulon ellenorizni a peldanyt mert az injector ugy is validal.
            //

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

            Built = true;
        }

        public override Lifetime? Lifetime { get; } = Lifetime.Instance;

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;

        public override bool SetInstance(IServiceReference reference) =>
            //
            // Peldany eseten ez a metodus elvileg sose kerulhet meghivasra.
            //

            throw new NotImplementedException();
    }
}