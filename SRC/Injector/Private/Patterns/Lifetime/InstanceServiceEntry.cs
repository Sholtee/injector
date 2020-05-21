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
        public InstanceServiceEntry(Type @interface, string? name, object instance, bool releaseOnDispose, IServiceContainer owner) : base(
            @interface, 
            name, 
            owner)
        {
            //
            // Nem kell kulon ellenorizni a peldanyt mert a ServiceReference.SetValue() validal.
            //

            Instance = new ServiceReference(this, instance, externallyOwned: !releaseOnDispose);

            //
            // Ez kivetelt fog dobni ha "@interface"-en akar csak egy aspektus is van (peldanynak nincs
            // factory-ja -> nem lehet proxy-zni).
            //

            this.ApplyAspects();
        }

        public override bool SetInstance(IServiceReference reference, IReadOnlyDictionary<string, object> options) =>
            //
            // Peldany eseten ez a metodus elvileg sose kerulhet meghivasra.
            //

            throw new NotImplementedException();
    }
}