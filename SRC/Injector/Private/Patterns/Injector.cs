/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;
    using Properties;

    internal partial class Injector : ServiceContainer, IInjector, IScopeFactory
    {
        private readonly IServicePath FPath = new ServicePath();

        private readonly ExclusiveBlock FExclusiveBlock = new ExclusiveBlock();

        private readonly IReadOnlyDictionary<string, object> FOptions;

        private void CheckBreaksTheRuleOfStrictDI(AbstractServiceEntry requested)
        {
            if (!Config.Value.Injector.StrictDI) return;

            AbstractServiceEntry? requestor = FPath.Requestor?.RelatedServiceEntry; // lehet NULL

            //
            // Ha a fuggosegi fa gyokerenel vagyunk akkor a metodus nem ertelmezett.
            //

            if (requestor == null) return;

            //
            // A kerelmezett szerviznek legalabb addig kell leteznie mint a kerelmezo szerviznek.
            //

            if (requested.Lifetime!.CompareTo(requestor.Lifetime!) < 0)
            {
                var ex = new RequestNotAllowedException(Resources.STRICT_DI);
                ex.Data[nameof(requestor)] = requestor;
                ex.Data[nameof(requested)] = requested;

                throw ex;
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

                Debug.Assert(FPath.Requestor?.Value == null, "Already produced services can not request dependencies");

                const string path = nameof(path);

                try
                {
                    //
                    // Bejegyzes lekerdezese, generikus bejegyzes tipizalasat megengedjuk. A "QueryModes.ThrowOnError" 
                    // miatt "entry" tuti nem NULL.
                    //

                    AbstractServiceEntry requestedEntry = Get(iface, name, QueryModes.AllowSpecialization | QueryModes.ThrowOnError)!;

                    CheckBreaksTheRuleOfStrictDI(requestedEntry);

                    IServiceReference requested = Resolve(requestedEntry);

                    //
                    // Ha a szervizt egy masik szerviz igenyelte akkor annak fuggosegei koze felvesszuk az ujonan letrehozott peldanyt.
                    //

                    FPath.Requestor?.AddDependency(requested); // TODO: Ezt atmozgatni a Instantiate()-be

                    return requested;
                }

                //
                // Csak ott bovitjuk a kivetelt ahol az dobva volt (ez a metodus lehet rekurzivan hivva).
                //

                catch (ServiceNotFoundException e) when (e.Data[path] is null)
                {
                    e.Data[path] = ServicePath.Format
                    (
                        FPath
                            .Select(node => (IServiceId) node.RelatedServiceEntry)
                            .Append(new ServiceId(iface, name))
                    );
                    throw;
                }
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