/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    //                                        !!!FIGYELEM!!!
    //
    // Ez az osztaly kozponti komponens, ezert minden modositast korultekintoen, a teljesitmenyt szem elott tartva
    // kell elvegezni:
    // - nincs Sysmte.Linq
    // - nincs System.Reflection
    // - mindig futtassuk a teljesitmeny teszteket (is) hogy a hatekonysag nem romlott e
    //

    internal class Injector : ConcurrentServiceRegistry, IInjector, IScopeFactory, ICaptureDisposable
    {
        private readonly ServicePath FPath = new();

        //
        // - Azert Stack<> hogy forditott iranyban szabaditsuk fel a szervizeket mint ahogy letrehoztuk oket (igy az
        //   eppen felszabaditas alatt levo szerviz meg tudja hivatkozni a fuggosegeit).
        // - Ne Stack<IDisposable> legyen h tamogassuk azt a perverz esetet is ha egy szerviz csak az
        //   IAsyncDisposable-t valositja meg.
        //

        private readonly Stack<object> FCapturedDisposables = new(capacity: 5);

        private readonly object FRequestLock = new();

        private bool FDisposing;

        private object GetOrCreateInstance(AbstractServiceEntry requested)
        {
            //
            // 1. eset: Csak egy peldanyt kell letrehozni amit vki korabban mar megtett [HasFlag(Built)]
            //    - Visszaadjuk azt (a szerviz tulajdonosa itt lenyegtelen)
            //    - A szerviz felszabaditasaval mar nem kell foglalkoznunk
            //

            if (requested.State.HasFlag(ServiceEntryStates.Built))
                return requested.GetSingleInstance();

            //
            // 2. eset: Nem mi vagyunk a tulajdonosok, ertesitjuk a tulajdonost h hozza letre o a bejegyzest
            //

            if (requested.Owner is not null && requested.Owner != this)
                return ((Injector) requested.Owner).GetOrCreateInstance(requested);

            //
            // 3. eset: Uj peldanyt kell letrehozni.
            //    - Itt a teljes lekerdezest zaroljuk ne csak a "requested" bejegyzest (ha megosztott szerviz hivatkozik megosztottra
            //      korkorosen es parhuzamosan probaljuk a ket szervizt legyartani kesz is a dead lock).
            //    - A lock() jol kezeli azt az esetet is ha rekurzivan kerul ez a metodus meghivasra.
            //

            lock (FRequestLock)
            {
                //
                // Kozben mar lehet be lett allitva
                //

                if (requested.State.HasFlag(ServiceEntryStates.Built))
                    return requested.GetSingleInstance();

                //
                // - StrictDI ellenorzes csak akkor van ha korabban meg nem tudtuk peldanyositani a szervizt (ha korabban
                //   mar tudtuk peldanyositani akkor ez az ellenorzes is mar megtortent).
                // - Ha a fuggosegi fa gyokerenel vagyunk akkor a metodus nem ertelmezett.
                //

                if (Options.StrictDI && !requested.State.HasFlag(ServiceEntryStates.Instantiated) && FPath.Count > 0)
                {
                    AbstractServiceEntry requestor = FPath[^1];

                    //
                    // A kerelmezett szerviznek legalabb addig kell leteznie mint a kerelmezo szerviznek.
                    //

                    if (requested.Lifetime!.CompareTo(requestor.Lifetime!) < 0)
                    {
                        RequestNotAllowedException ex = new(Resources.STRICT_DI);
                        ex.Data[nameof(requestor)] = requestor;
                        ex.Data[nameof(requested)] = requested;

                        throw ex;
                    }
                }

                object instance;

                //
                // 1) Korabban mar peldanyositott szervizt igenylunk -> Nem bovitjuk az utvonalat a szerviz iranyaban,
                //    ugy sem lesz CDEP (peldanyositott szerviznek minden fuggosege is mar peldanyositott)
                // 2) Korabban meg nem peldanyositottuk a szervizt -> Bovitjuk az utvonalat a szerviz iranyaban
                //    a) 1 hosszu utnal -> nincs CDEP ellenorzes
                //    b) Kulonben -> CDEP ellenorzes 
                //

                if (!requested.State.HasFlag(ServiceEntryStates.Instantiated))
                {
                    FPath.Push(requested);
                    try
                    {
                        FPath.CheckNotCircular(); // egy elemnel nem csinal semmit
                        instance = requested.CreateInstance(this);
                    }
                    finally
                    {
                        FPath.Pop();
                    }
                }
                else
                    instance = requested.CreateInstance(this);

                //
                // Ellenorizzuk h az ujonan letrehozott peldanyt kesobb fel kell e szabaditani
                //

                if (instance is IDisposable || instance is IAsyncDisposable)
                    FCapturedDisposables.Push(instance); // nem szal biztos -> lock-on belul legyen

                return instance;
            }
        }

        private object? GetOrCreateInstance(Type iface, string? name, bool throwOnMissing)
        {
            CheckNotDisposed();

            if (FDisposing)
                throw new InvalidOperationException(Resources.INJECTOR_IS_BEING_DISPOSED);

            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            AbstractServiceEntry requested = GetEntry(iface, name); // szal biztos

            if (requested is MissingServiceEntry && !throwOnMissing)
                return null;

            object instance = GetOrCreateInstance(requested); // szal biztos

            if (requested is IRequiresServiceAccess accessor)
                instance = accessor.ServiceAccess(instance);

            if (!requested.Interface.IsInstanceOfType(instance))
                throw new InvalidCastException(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, requested.Interface));

            return instance;
        }

        protected virtual Injector CreateDerived() => new Injector(this);

        protected override IReadOnlyCollection<AbstractServiceEntry> BuiltInServices => new AbstractServiceEntry[]
        {
            new ContextualServiceEntry(typeof(IServiceRegistry), null, owner => owner),
            new ContextualServiceEntry(typeof(IInjector), null,  owner => owner),
            new ContextualServiceEntry(typeof(IServicePath), null,  owner => ((Injector) owner).FPath),
            
            //
            // ScopaFactory-nak mindig a gyoker scope-ot hasznaljuk (ez a getter csak gyoker scope-ban
            // kerul meghivasra)
            //

            new ContextualServiceEntry(typeof(IScopeFactory), null, owner => this),

            //
            // Ne Scoped legyen h StrictDI ne anyazzon
            //

            new ContextualServiceEntry(typeof(IDictionary<string, object?>), IInjectorBasicExtensions.META_NAME, _ => new Dictionary<string, object?>()), 
            new ScopedServiceEntry(typeof(IEnumerable<>), null, typeof(ServiceEnumerator<>), null!)
        };

        protected override void BeforeDispose()
        {
            FDisposing = true;
            base.BeforeDispose();
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                while (FCapturedDisposables.Count > 0)
                {
                    switch (FCapturedDisposables.Pop())
                    {
                        case IDisposable disposable:
                            disposable.Dispose();
                            break;

                        //
                        // Tamogassuk azt a pervezs esetet ha egy szerviz csak az IAsyncDisposable interface-t valositja meg.
                        //

                        case IAsyncDisposable asyncDisposable:
                            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                            break;
                    }
                }
            }

            base.Dispose(disposeManaged);         
        }

        protected override async ValueTask AsyncDispose()
        {
            while (FCapturedDisposables.Count > 0)
            {
                switch (FCapturedDisposables.Pop())
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync();
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }

            //
            // Nem kell "base" hivas mert az a Dispose()-t hivja
            //
        }

        public Injector(ISet<AbstractServiceEntry> entries, ScopeOptions options, CancellationToken cancellation) : base(entries, cancellation: cancellation)
        {
            Options = options;
        }

        public Injector(Injector parent) : base(parent)
        {
            Options = parent.Options;
        }

        #region IScopeFactory
        public IInjector CreateScope()
        {
            CheckNotDisposed();

            if (FDisposing)
                throw new InvalidOperationException(Resources.INJECTOR_IS_BEING_DISPOSED);

            return CreateDerived();
        }
        #endregion

        #region ICaptureDisposable
        public IReadOnlyCollection<object> CapturedDisposables => FCapturedDisposables;
        #endregion

        #region IInjector
        public object Get(Type iface, string? name) => GetOrCreateInstance(iface, name, throwOnMissing: true)!;

        public object? TryGet(Type iface, string? name) => GetOrCreateInstance(iface, name, throwOnMissing: false);

        public ScopeOptions Options { get; }
        #endregion
    }
}
