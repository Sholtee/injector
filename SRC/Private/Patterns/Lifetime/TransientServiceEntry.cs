/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a transient service entry.
    /// </summary>
    internal class TransientServiceEntry : ProducibleServiceEntry
    {
        public TransientServiceEntry(Type @interface, Func<IInjector, Type, object> factory, ICollection<ServiceEntry> owner) : base(@interface, DI.Lifetime.Transient, factory, owner)
        {
        }

        public TransientServiceEntry(Type @interface, Type implementation, ICollection<ServiceEntry> owner) : base(@interface, DI.Lifetime.Transient, implementation, owner)
        {
        }

        public TransientServiceEntry(Type @interface, ITypeResolver implementation, ICollection<ServiceEntry> owner) : base(@interface, DI.Lifetime.Transient, implementation, owner)
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

        public override object Clone() => new TransientServiceEntry(Interface, Factory, null)
        {
            //
            // Ne az Implementation-t magat adjuk at h a resolver ne triggerelodjon ha 
            // meg nem volt hivva.
            //

            FImplementation = FImplementation
        };

        public override ServiceEntry CopyTo(ICollection<ServiceEntry> target)
        {
            var result = new TransientServiceEntry(Interface, Factory, target)
            {
                FImplementation = FImplementation
            };
            target?.Add(result);
            return result;
        }
    }
}