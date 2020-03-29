/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes a transient service entry.
    /// </summary>
    internal class TransientServiceEntry : ProducibleServiceEntry
    {
        private ServiceReferenceCollection? FSpawnedServices;

        //
        // A legyartott szervizek listajat az elso hasznalat elott hozzuk letre. Ennek foleg a teljestimenyteszteknel 
        // van ertelme ahol a szervizbejegyzesek nem biztos h fel vannak szabaditva (felszabaditas ideje ne szamitson 
        // bele a tesztbe). Igy levehetunk nemi terhet a GC vallarol.
        //

        internal ServiceReferenceCollection SpawnedServices => FSpawnedServices ??= new ServiceReferenceCollection(); 

        private void EnsureNotFull(IReadOnlyDictionary<string, object> options) 
        {
            int? threshold = options?.GetValueOrDefault<int?>("MaxSpawnedTransientServices");

            if (SpawnedServices.Count >= threshold)
                //
                // Ha ide jutunk az azt jelenti h jo esellyel a tartalmazo injector ujrahasznositasra kerult
                // (ahogy az a teljesitmeny teszteknel meg is tortent).
                //

                throw new Exception(string.Format(Resources.Culture, Resources.INJECTOR_SHOULD_BE_RELEASED, threshold));
        }

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
            EnsureAppropriateReference(reference);
            EnsureProducible();
            EnsureNotFull(options);

            IInjector relatedInjector = Ensure.IsNotNull(reference.RelatedInjector, $"{nameof(reference)}.{nameof(reference.RelatedInjector)}");
            Ensure.AreEqual(relatedInjector.UnderlyingContainer, Owner, Resources.INAPPROPRIATE_OWNERSHIP);

            //
            // "Factory" biztos nem NULL [lasd EnsureProducible()]
            //

            reference.Value = Factory!(relatedInjector, Interface);

            //
            // Mivel "TransientServiceEntry.Instance" mindig NULL ezert ide annyi alkalommal jutunk el
            // ahanyszor a szervizt igenylik -> A megfelelo elettartam kezeles vegett egy belso listaban
            // tartjuk szamon az eddig igenyelt peldanyokat.
            //
            // Az Add() hivas megnoveli a "reference" referenciaszamlalojat ezert a Release() hivas (h a
            // szamlalo az eredeti erteken alljon).
            //

            SpawnedServices.Add(reference);   
            reference.Release();

            Debug.Assert(!reference.Disposed);

            return true;
        }

        public override AbstractServiceEntry CopyTo(IServiceContainer target)
        {
            Ensure.Parameter.IsNotNull(target, nameof(target));
            Ensure.NotDisposed(this);

            var result = new TransientServiceEntry(this, target);
            target.Add(result);
            return result;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) FSpawnedServices?.Dispose();

            base.Dispose(disposeManaged);
        }

        protected async override ValueTask AsyncDispose()
        {
            if (FSpawnedServices != null)
                await FSpawnedServices.DisposeAsync();

            //
            // Nem kell "base" hivas mert az a standard Dispose()-t hivna.
            //
        }
    }
}