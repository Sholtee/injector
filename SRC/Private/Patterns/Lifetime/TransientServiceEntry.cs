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
        public TransientServiceEntry(Type @interface,  Func<IInjector, Type, object> factory) : base(@interface, DI.Lifetime.Transient, factory)
        {
        }

        public TransientServiceEntry(Type @interface, Type implementation) : base(@interface, DI.Lifetime.Transient, implementation)
        {
        }

        public TransientServiceEntry(Type @interface, ITypeResolver implementation) : base(@interface, DI.Lifetime.Transient, implementation)
        {
        }

        public override object Value => null;

        public override object GetService(IInjector injector) => Factory(injector, Interface);

        public override object Clone() => new TransientServiceEntry(Interface, Factory)
        {
            FImplementation = FImplementation
        };
    }
}