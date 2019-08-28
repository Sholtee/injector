/********************************************************************************
* ScopedServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes a scoped service entry.
    /// </summary>
    internal class ScopedServiceEntry : ProducibleServiceEntry
    {
        private object FValue;

        public ScopedServiceEntry(Type @interface, Func<IInjector, Type, object> factory, ServiceCollection owner) : base(@interface, DI.Lifetime.Scoped, factory, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, Type implementation, ServiceCollection owner) : base(@interface, DI.Lifetime.Scoped, implementation, owner)
        {
        }

        public ScopedServiceEntry(Type @interface, ITypeResolver implementation, ServiceCollection owner) : base(@interface, DI.Lifetime.Scoped, implementation, owner)
        {
        }

        public override object Value => FValue;

        public override object GetService(IInjector injector, Type iface = null)
        {
            CheckProducible();

            if (iface != null && iface != Interface)
                throw new NotSupportedException(Resources.NOT_SUPPORTED);

            return FValue ?? (FValue = Factory(injector, Interface));
        }

        public override ServiceEntry CopyTo(ServiceCollection target)
        {
            var result = new ScopedServiceEntry(Interface, Factory, target)
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

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                (FValue as IDisposable)?.Dispose();
                FValue = null;
            }
            base.Dispose(disposeManaged);
        }
    }
}