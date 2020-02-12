/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes a transient service entry.
    /// </summary>
    internal class TransientServiceEntry : ProducibleServiceEntry
    {
        private readonly ServiceReferenceCollection FSpawnedServices = new ServiceReferenceCollection();

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

        public override bool SetInstance(ServiceReference reference, IReadOnlyDictionary<string, object> options)
        {
            CheckProducible();

            Debug.Assert(reference.RelatedInjector.UnderlyingContainer == Owner);
   
            int? threshold = options?.GetValueOrDefault<int?>("MaxSpawnedTransientServices");

            if (FSpawnedServices.Count >= threshold)
                //
                // Ha ide jutunk az azt jelenti h jo esellyel a tartalmazo injector ujrahasznositasra kerult
                // (ahogy az a teljesitmeny teszteknel meg is tortent).
                //

                throw new Exception(string.Format(Resources.Culture, Resources.INJECTOR_SHOULD_BE_RELEASED, threshold));

            reference.Value = Factory(reference.RelatedInjector, Interface);

            //
            // Mivel "TransientServiceEntry.Instance" mindig NULL ezert ide annyi alkalommal jutunk el
            // ahanyszor a szervizt igenylik -> A megfelelo elettartam kezeles vegett egy belso listaban
            // tartjuk szamon az eddig igenyelt peldanyokat.
            //
            // Az Add() hivas megnoveli a "reference" referenciaszamlalojat ezert a Release() hivas (h a
            // szamlalo 1-en alljon).
            //

            FSpawnedServices.Add(reference);   
            reference.Release();

            Debug.Assert(reference.RefCount == 1);

            return true;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            CheckDisposed();

            var result = new TransientServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) FSpawnedServices.Dispose();

            base.Dispose(disposeManaged);
        }
    }
}