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
        private List<IDisposableEx> FServicesToDispose;

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

        public override object GetService(Func<IInjector> injectorFactory)
        {
            CheckProducible();

            object result = Factory(injectorFactory(), Interface);

            if (result is IDisposable)
            {
                if (FServicesToDispose == null) FServicesToDispose = new List<IDisposableEx>(1);

                Type wrapper = Cache<Type, Type>.GetOrAdd(Interface, () =>
                {
                    var gen = (ITypeGenerator) typeof(GeneratedDisposable<>).MakeGenericType(Interface).CreateInstance(Array.Empty<Type>());
                    return gen.Type;
                });

                result = wrapper.CreateInstance(new[] { Interface }, result);

                FServicesToDispose.Add((IDisposableEx) result);
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
                FServicesToDispose.ForEach(svc => 
                {
                    if (!svc.Disposed) 
                    {
                        svc.Dispose();
                    }
                });
                FServicesToDispose = null;
            }

            base.Dispose(disposeManaged);
        }
    }
}