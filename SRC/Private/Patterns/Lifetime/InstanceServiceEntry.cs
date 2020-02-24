/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes an instance service entry.
    /// </summary>
    internal class InstanceServiceEntry : AbstractServiceEntry
    {
        public InstanceServiceEntry(Type @interface, string name, object instance, bool releaseOnDispose, IServiceContainer owner) : base(
            @interface, 
            name, 
            lifetime: null, 
            Ensure.Parameter.IsNotNull(owner, nameof(owner)))
        {
            //
            // Nem kell kulon ellenorizni a peldanyt mert a ServiceReference.SetValue() validal.
            //

            Instance = new ServiceReference(this, null) 
            { 
                Value = Ensure.Parameter.IsNotNull(instance, nameof(instance))
            };

            if (!releaseOnDispose) Instance.SuppressDispose();
        }

        public override bool SetInstance(ServiceReference reference, IReadOnlyDictionary<string, object> options) =>
            //
            // Peldany eseten ez a metodus elvileg sose kerulhet meghivasra.
            //

            throw new NotImplementedException();
    }
}