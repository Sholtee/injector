/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes a transient service entry.
    /// </summary>
    internal class TransientServiceEntry : ProducibleServiceEntry
    {
        public TransientServiceEntry(Type @interface, Func<IInjector, Type, object> factory, ServiceCollection owner) : base(@interface, DI.Lifetime.Transient, factory, owner)
        {
        }

        public TransientServiceEntry(Type @interface, Type implementation, ServiceCollection owner) : base(@interface, DI.Lifetime.Transient, implementation, owner)
        {
        }

        public TransientServiceEntry(Type @interface, ITypeResolver implementation, ServiceCollection owner) : base(@interface, DI.Lifetime.Transient, implementation, owner)
        {
        }

        public override object Value => null;

        public override object GetService(IInjector injector, Type iface = null)
        {
            CheckProducible();

            if (iface != null)
            {
                Type requested = iface;

                if (requested.IsGenericTypeDefinition())
                    throw new ArgumentException(Resources.CANT_INSTANTIATE_GENERICS, nameof(iface));

                //
                // Generikus interface-hez tartozo factory-nal megengedjuk specializalt peldany lekerdezeset.
                // Megjegyzes: a GetGenericTypeDefinition() dobhat kivetelt ha "iface" nem generikus.
                //

                if (IsFactory && Interface.IsGenericTypeDefinition())
                    requested = requested.GetGenericTypeDefinition();

                //
                // Minden mas esetben csak a regisztralt szervizt kerdezhetjuk le.
                //

                if (requested != Interface)
                    throw new NotSupportedException(Resources.NOT_SUPPORTED);
            }

            return Factory(injector, iface ?? Interface);
        }

        public override ServiceEntry CopyTo(ServiceCollection target)
        {
            var result = new TransientServiceEntry(Interface, Factory, target)
            {
                //
                // Ne az Implementation-t magat adjuk at h a resolver ne triggerelodjon ha 
                // meg nem volt hivva.
                //

                FImplementation = FImplementation
            };
            target.Add(result);
            return result;
        }
    }
}