/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly IReadOnlyDictionary<string, object> FOptions;

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

        public Injector(IServiceContainer parent, IReadOnlyDictionary<string, object>? options) : base(Ensure.Parameter.IsNotNull(parent, nameof(parent)))
        {
            FOptions = (options ?? new Dictionary<string, object>()).Extend(nameof(Config.Value.Injector.MaxSpawnedTransientServices), Config.Value.Injector.MaxSpawnedTransientServices);
            
            UnderlyingContainer
                .Instance<IInjector>(this)
                .Instance<IScopeFactory>(this)
                .Instance(FPath)
                .Instance($"{INTERNAL_SERVICE_NAME_PREFIX}options", FOptions);

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
        public IServiceReference GetReference(Type iface, string? name)
        {
            CheckNotDisposed();

            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            using (FExclusiveBlock.Enter())
            {
                //
                // Ha vkinek a fuggosege vagyunk akkor a fuggo szerviz itt meg nem lehet legyartva.
                //

                Debug.Assert(FPath.Requestor?.Value is null, "Already produced services can not request dependencies");

                return Resolve(iface, name);
            }
        }

        public object Get(Type iface, string? name) 
        {
            CheckNotDisposed();

            IServiceReference reference = GetReference(iface, name);

            object instance = reference.GetInstance();

            if (!iface.IsInstanceOfType(instance))
                throw new InvalidCastException(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, iface));

            return instance;
        }

        public object? TryGet(Type iface, string? name) 
        {
            CheckNotDisposed();

            try
            {
                return Get(iface, name);
            }
            catch(ServiceNotFoundException) 
            {
                return null;
            }
        }

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
        public virtual Injector CreateScope(IReadOnlyDictionary<string, object>? options) => new Injector(Parent!, options);

        public virtual Injector CreateScope(IServiceContainer parent, IReadOnlyDictionary<string, object>? options) => new Injector(parent, options);

        IInjector IScopeFactory.CreateScope(IReadOnlyDictionary<string, object>? options) => CreateScope(options);

        IInjector IScopeFactory.CreateScope(IServiceContainer parent, IReadOnlyDictionary<string, object>? options) => CreateScope(parent, options);
        #endregion

        #region Composite
        public override ICollection<IServiceContainer> Children { get; } = Array.Empty<IServiceContainer>();
        #endregion
    }
}