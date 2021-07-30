/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;
    using Properties;

    internal partial class Injector : ServiceContainer, IInjector, IScopeFactory
    {
        private readonly IServicePath FPath = new ServicePath();

        private readonly ExclusiveBlock FExclusiveBlock = new();

        private IServiceReference? GetReferenceInternal(Type iface, string? name, QueryModes queryModes)
        {
            CheckNotDisposed();

            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            using (FExclusiveBlock.Enter())
            {
                return Resolve(iface, name, queryModes);
            }
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
                FExclusiveBlock.Dispose();

            base.Dispose(disposeManaged);
        }

        protected override async ValueTask AsyncDispose()
        {
            await base.AsyncDispose();
            await FExclusiveBlock.DisposeAsync();    
        }

        public Injector(IServiceContainer parent) : base(Ensure.Parameter.IsNotNull(parent, nameof(parent)))
        {
            UnderlyingContainer
                .Instance<IInjector>(this)
                .Instance<IScopeFactory>(this)
                .Instance(FPath);

            this.RegisterServiceEnumerator();
        }

        public override void Add(AbstractServiceEntry entry)
        {
            Ensure.Parameter.IsNotNull(entry, nameof(entry));

            //
            // Injector nem hasznalhato absztrakt bejegyzesekkel.
            //

            if (entry.GetType() == typeof(AbstractServiceEntry))
            {
                var ioex = new InvalidOperationException(Resources.INVALID_INJECTOR_ENTRY);
                ioex.Data[nameof(entry)] = entry;

                throw ioex;
            }

            base.Add(entry);
        }

        #region IInjector
        public IServiceReference GetReference(Type iface, string? name) => GetReferenceInternal(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnMissing)!;

        public IServiceReference? TryGetReference(Type iface, string? name) => GetReferenceInternal(iface, name, QueryModes.AllowSpecialization);

        public object Get(Type iface, string? name) => GetReference(iface, name).GetInstance();

        public object? TryGet(Type iface, string? name) => TryGetReference(iface, name)?.GetInstance();

        public IServiceContainer UnderlyingContainer 
        {
            get
            {
                CheckNotDisposed();
                return this;
            }
        }
        #endregion

        #region IScopeFactory
        public virtual Injector CreateScope()
        {
            CheckNotDisposed();
            return new Injector((IServiceContainer) Parent!);
        }

        public virtual Injector CreateScope(IServiceContainer parent)
        {
            CheckNotDisposed();
            return new Injector(parent);
        }

        IInjector IScopeFactory.CreateScope() => CreateScope();

        IInjector IScopeFactory.CreateScope(IServiceContainer parent) => CreateScope(parent);
        #endregion

        #region Composite
        public override ICollection<IServiceContainer> Children { get; } = Array.Empty<IServiceContainer>();
        #endregion
    }
}