/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Globalization;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes a transient service entry.
    /// </summary>
    internal class TransientServiceEntry : ProducibleServiceEntry
    {
        private readonly ServiceCollection FSpawnedServices = new ServiceCollection();

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

        public override void GetService(IInjector injector, ref ServiceReference reference)
        {
            try
            {
                CheckProducible();

                if (FSpawnedServices.Count == Config.Value.MaxSpawnedServices)
                    //
                    // Ha ide jutunk az azt jelenti h jo esellyel a tartalmazo injector ujrahasznositasra kerult
                    // (es igy siman megehetjuk a rendelkezesre allo memoriat ahogy az a teljesitmeny teszteknel
                    // meg is tortent).
                    //

                    throw new Exception(string.Format(CultureInfo.InvariantCulture, Resources.INJECTOR_SHOULD_BE_RELEASED, Config.Value.MaxSpawnedServices));

                reference.Instance = Factory(injector, Interface);

                FSpawnedServices.Add(reference); // inkrementalja "reference" referenciaszamlalojat
            }
            finally
            {
                //
                // Az FSpawnedServices kezeli az elettartamot ha mar hozza lett adva, kivetel eseten pedig
                // amugy is fel kell szabaditani.
                //

                reference.Release();
            }
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

    public partial class Config 
    {
        /// <summary>
        /// The maximum number of <see cref="Lifetime.Transient"/> service instances can be held by the <see cref="IInjector"/>.
        /// </summary>
        public int MaxSpawnedServices { get; set; } = 512;
    }
}