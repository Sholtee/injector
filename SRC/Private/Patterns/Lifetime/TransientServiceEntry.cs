/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a transient service entry.
    /// </summary>
    public class TransientServiceEntry : ProducibleServiceEntry
    {
        public TransientServiceEntry(Type @interface, Lifetime? lifetime, Func<IInjector, Type, object> factory) : base(@interface, lifetime, factory)
        {
        }

        public TransientServiceEntry(Type @interface, Lifetime? lifetime, Type implementation) : base(@interface, lifetime, implementation)
        {
        }

        public TransientServiceEntry(Type @interface, Lifetime? lifetime, ITypeResolver implementation) : base(@interface, lifetime, implementation)
        {
        }

        public override object Value => null;

        public override object GetService(IInjector injector) => Factory(injector, Interface);

        public override object Clone() => new TransientServiceEntry(Interface, Lifetime, Factory)
        {
            FImplementation = FImplementation
        };
    }
}