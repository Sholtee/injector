/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a transient service entry.
    /// </summary>
    internal class TransientServiceEntry : ProducibleServiceEntry
    {
        private List<IDisposable> FServicesToDispose;

        private TransientServiceEntry(TransientServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Transient, factory, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string name, Type implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Transient, implementation, owner)
        {
        }

        public TransientServiceEntry(Type @interface, string name, ITypeResolver implementation, IServiceContainer owner) : base(@interface, name, DI.Lifetime.Transient, implementation, owner)
        {
        }

        public override object Value => null;

        public override object GetService(Func<IInjector> injectorFactory, FactoryOptions options)
        {
            CheckProducible();

            object result = Factory(injectorFactory(), Interface);
            
            if (options.HasFlag(FactoryOptions.ForceAutoDispose)) 
            {
                IDisposable disposable = result as IDisposable;
                if (result != null)
                {
                    if (FServicesToDispose == null) FServicesToDispose = new List<IDisposable>(1);
                    FServicesToDispose.Add(disposable);
                }
            }

            return result;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            var result = new TransientServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (FServicesToDispose != null) 
            {
                FServicesToDispose.ForEach(svc => svc.Dispose());
                FServicesToDispose = null;
            }

            base.Dispose(disposeManaged);
        }
    }
}