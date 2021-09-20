﻿/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;
    using Properties;

    internal class Injector : ServiceRegistry, IInjector, ICaptureDisposable
    {
        #region Private
        private readonly ExclusiveBlock? FExclusiveBlock;

        private readonly ServicePath? FPath;

        private bool FDisposing;

        //
        // - Azert Stack<> hogy forditott iranyban szabaditsuk fel a szervizeket mint ahogy letrehoztuk oket (igy az
        //   eppen felszabaditas alatt levo szerviz meg tudja hivatkozni a fuggosegeit).
        // - Ne Stack<IDisposable> legyen h tamogassuk azt a perverz esetet is ha egy szerviz csak az
        //   IAsyncDisposable-t valositja meg.
        //

        private Stack<object>? FCapturedDisposables;
        #endregion

        #region Internal
        internal object GetInternalUnsafe(AbstractServiceEntry requested)
        {
            //
            // 1. eset: Csak egy peldanyt kell letrehozni amit vki korabban mar megtett [HasFlag(Built)] -> visszaadjuk azt.
            //          Ebben az esetben a szerviz felszabaditasaval mar nem kell foglalkoznunk.
            //

            if (requested.State.HasFlag(ServiceEntryStates.Built))
                return requested.GetSingleInstance();

            //
            // 2. eset: Uj peldanyt kell letrehozni de a bejegyzes megosztott ezert dedikalt scope kell neki
            //          Megjegyzesek:
            //            - Megosztott bejegyzesek injector peldanyok kozt ertelmezettek ezert minden muveletnek exkluzivnak kell lennie.
            //            - A Monitor.IsEntered() vizsgalat azert kell h lassuk ha az aktualis szal korabban mar elkezdte feldolgozni a
            //              szerviz igenylest. Ez a megoldas jol kezeli azt az esetet ha megosztott bejegyzes hivatkozik sajat magara
            //              -> nem lesz S.O.E.
            //

            if (requested.IsShared && !Monitor.IsEntered(requested))
            {
                //
                // ServiceEntry-t zaroljuk h a lock injectorok kozt is ertelmezve legyen.
                // 

                lock (requested)
                {
                    //
                    // A lock miatt lehet h kozben vki mar beallitotta.
                    //

                    if (requested.State.HasFlag(ServiceEntryStates.Built))
                        return requested.GetSingleInstance();

                    //
                    // Letrehozunk egy dedikalt injector-t aminek a felszabaditasa "Parent" feladata lesz
                    //   -> Ennek az injector peldanynak a felszabaditasa nem befolyasolja a szerviz elettartamat.
                    //

                    Injector dedicatedInjector = (Injector) Parent.CreateSystemScope();

                    try
                    {
                        //
                        // Ugrunk a 3. esetre
                        //

                        return dedicatedInjector.GetInternalUnsafe(requested);
                    }
                    catch
                    {
                        dedicatedInjector.Dispose();
                        throw;
                    }
                }
            }

            //
            // 3. eset: Uj peldanyt kell letrehozni es ezt az aktualis scope meg is teheti
            //

            object instance;

            if (Options.SafeMode && FPath!.First?.State.HasFlag(ServiceEntryStates.Instantiated) is not true)
            {
                //
                // Ezt a logikat NE az overload-ban hivjuk hogy a "requested" bejegyzes az ujonan letrehozott
                // scope FPath-aban keruljon regisztralasra.
                //

                FPath.Push(requested);
                try
                {
                    FPath.CheckNotCircular();
                    instance = requested.CreateInstance(this);
                }
                finally
                {
                    FPath.Pop();
                }
            }
            else
                //
                // Ha korabban mar tudtuk peldanyositani a szervizt vagy pedig nem biztonsagos modban vagyunk akkor nincs
                // korkoros referencia ellenorzes.
                //

                instance = requested.CreateInstance(this);

            //
            // Ellenorizzuk h az ujonan letrehozott peldanyt kesobb fel kell e szabaditani
            //

            if (instance is IDisposable || instance is IAsyncDisposable)
                CaptureDisposable(instance);

            return instance;
        }

        internal object? GetInternalUnsafe(Type iface, string? name, bool throwOnMissing)
        {
            AbstractServiceEntry? requested = GetEntry(iface, name);

            if (requested is null)
            {
                if (throwOnMissing)
                {
                    ServiceId id = new(iface, name);
                    
                    ServiceNotFoundException ex = new
                    (
                        string.Format(Resources.Culture, Resources.SERVICE_NOT_FOUND, id.FriendlyName())
                    );
   
                    //
                    // Csak az "igenylo -> igenyelt" parost adjuk vissza.
                    //

                    AbstractServiceEntry? requestor = FPath?.Last; // unsafe modban nincs utvonal
                    if (requestor is not null)
                        ex.Data["path"] = ServicePath.Format(new IServiceId[] {requestor, id});

                    throw ex;
                }

                return null;
            }

            if (Options.StrictDI)
            {
                AbstractServiceEntry? requestor = FPath?.Last; // unsafe modban nincs utvonal

                //
                // - Ha a fuggosegi fa gyokerenel vagyunk akkor a metodus nem ertelmezett.
                // - A kerelmezett szerviznek legalabb addig kell leteznie mint a kerelmezo szerviznek.
                //

                if (requestor?.Lifetime is not null && requested.Lifetime?.CompareTo(requestor.Lifetime) < 0)
                {
                    RequestNotAllowedException ex = new(Resources.STRICT_DI);
                    ex.Data["requestor"] = requestor;
                    ex.Data["requested"] = requested;

                    throw ex;
                }
            }

            //
            // Fuggosegek feloldasa es peldanyositas (ez a metodus rekurzivan ismet meghivasra kerulhet)
            //

            object instance = GetInternalUnsafe(requested);

            if (requested.ServiceAccess is not null)
                instance = requested.ServiceAccess(instance);

            if (!requested.Interface.IsInstanceOfType(instance))
                throw new InvalidCastException(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, requested.Interface));

            return instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal object? GetInternal(Type iface, string? name, bool throwOnMissing)
        {
            CheckNotDisposed();

            if (FDisposing)
                throw new InvalidOperationException(Resources.INJECTOR_IS_BEING_DISPOSED);

            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            using (FExclusiveBlock?.Enter()) // "using" nem szall el NULL-ra sem
            {
                return GetInternalUnsafe(iface, name, throwOnMissing);
            }
        }
        #endregion

        #region Protected
        protected override void BeforeDispose()
        {
            FDisposing = true;
            base.BeforeDispose();
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                while (FCapturedDisposables?.Count > 0)
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

                FExclusiveBlock?.Dispose();
            }

            base.Dispose(disposeManaged);         
        }

        protected override async ValueTask AsyncDispose()
        {
            while (FCapturedDisposables?.Count > 0)
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

            if (FExclusiveBlock is not null)
                await FExclusiveBlock.DisposeAsync();

            //
            // Nem kell "base" hivas mert az a Dispose()-t hivja
            //
        }
        #endregion

        public Injector(ScopeFactory parent, ScopeKind kind) : base(parent)
        {
            Parent = parent;
            Kind = kind;

            if (Options.SafeMode)
            {
                FExclusiveBlock = new ExclusiveBlock(ExclusiveBlockFeatures.SupportsRecursion);
                FPath = new ServicePath();
            }
        }

        public new ScopeFactory Parent { get; }

        public ScopeKind Kind { get; }

        #region ICaptureDisposable
        public IReadOnlyCollection<object> CapturedDisposables => ((IReadOnlyCollection<object>?) FCapturedDisposables) ?? Array.Empty<object>();

        public void CaptureDisposable(object obj)
        {
            if (Kind is ScopeKind.System)
                //
                // A rendszer scope-ok altal kozze tett szervizek egyszerre tobb scope-ban is hasznalva lehetnek (AbstractServiceEntry.IsShared)
                // ezert azok felszabaditasat a szulo vegzi, letrehozasuk forditott sorrendjeben (igy az eppen felszabaditas alatt levo szerviz
                // meg tudja hivatkozni a fuggosegeit).
                //

                Parent.CaptureDisposable(obj);
            else
            {
                FCapturedDisposables ??= new Stack<object>(capacity: 5);
                FCapturedDisposables.Push(obj);
            }
        }
        #endregion

        #region IInjector
        public object Get(Type iface, string? name) => GetInternal(iface, name, throwOnMissing: true)!;

        public object? TryGet(Type iface, string? name) => GetInternal(iface, name, throwOnMissing: false);

        public ScopeOptions Options => Parent.ScopeOptions;
        #endregion
    }
}
