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
    internal class TransientServiceEntry : ProducibleServiceEntry
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

        public override object GetService(IInjector injector, Type iface = null)
        {
            CheckProducible();

            if (iface != null)
                CheckInterfaceSupported(iface);

            return Factory(injector, iface ?? Interface);

        }

        public override object Clone() => new TransientServiceEntry(Interface, Factory)
        {
            //
            // Ne az Implementation-t magat adjuk at h a resolver ne triggerelodjon ha 
            // meg nem volt hivva.
            //

            FImplementation = FImplementation
        };
    }
}