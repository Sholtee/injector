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
        public InstanceServiceEntry(Type @interface, string name, object instance, bool releaseOnDispose, IServiceContainer owner) : base(@interface, name, null, owner ?? throw new ArgumentNullException(nameof(owner)))
        {
            //
            // Nem kell kulon ellenorizni a peldanyt mert a ServiceReference.SetValue() validal.
            //

            Instance = new ServiceReference(this, null) 
            { 
                Value = instance ?? throw new ArgumentNullException(nameof(instance))
            };

            if (!releaseOnDispose) Instance.SuppressDispose();
        }

        public override Func<IInjector, Type, object> Factory
        {
            //
            // Nem kell lekerdezeskor kivetelt dobni (Proxy() hivaskor ha a Factory null ugy is
            // hiba van).
            //

            get => null;
            set => throw new InvalidOperationException();
        }

        public override bool SetInstance(ServiceReference reference, IReadOnlyDictionary<string, object> options) =>
            //
            // Peldany eseten ez a metodus elvileg sose kerulhet meghivasra.
            //

            throw new NotImplementedException();
    }
}