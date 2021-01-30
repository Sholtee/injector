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
        public InstanceServiceEntry(Type @interface, string? name, object instance, bool externallyOwned, IServiceContainer owner) : base(
            @interface, 
            name, 
            owner)
        {
            Ensure.Parameter.IsNotNull(instance, nameof(instance));
            Ensure.Type.IsTypeOf(@interface, instance);

            //
            // Nem kell kulon ellenorizni a peldanyt mert a ServiceReference.SetValue() validal.
            //

            Instances = new[] 
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

        public override bool SetInstance(IServiceReference reference, IReadOnlyDictionary<string, object> options) =>
            //
            // Peldany eseten ez a metodus elvileg sose kerulhet meghivasra.
            //

            throw new NotImplementedException();
    }
}