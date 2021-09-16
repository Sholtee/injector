/********************************************************************************
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

    internal class Injector : ServiceRegistry, IInjector
    {
        #region Private
        private readonly ExclusiveBlock? FExclusiveBlock;

        private bool FDisposing;

        private ServicePath? FPath;

        private IList<object>? FProvidedDisposables;
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
            //              szerviz igenylest (nem mellesleg a lock(...) rekurzio eseten nem blokkolodik, igy ez a megoldas jol kezeli
            //              azt az esetet is ha megosztott bejegyzes hivatkozik sajat magara -> nem lesz S.O.E.).
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

            object result;

            //
            // Ha korabban mar tudtuk peldanyositani a szervizt vagy pedig nem biztonsagos modban vagyunk akkor nics
            // korkoros referencia ellenorzes.
            //

            if (!Options.SafeMode || FPath?.First?.State.HasFlag(ServiceEntryStates.Instantiated) is true)
                result = requested.CreateInstance(this);
            else
            {
                FPath ??= new ServicePath();

                FPath.Push(requested);
                try
                {
                    FPath.CheckNotCircular();
                    result = requested.CreateInstance(this);
                }
                finally
                {
                    FPath.Pop();
                }
            }

            //
            // Ellenorizzuk h az ujonan letrehozott peldanyt kesobb fel kell e szabaditani
            //

            if (result is IDisposable || result is IAsyncDisposable)
            {
                FProvidedDisposables ??= new List<object>(capacity: 5);
                FProvidedDisposables.Add(result);
            }

            return result;
        }

        internal object? GetInternalUnsafe(Type iface, string? name, bool throwOnMissing)
        {
            AbstractServiceEntry? requested = GetEntry(iface, name);

            if (requested is null)
            {
                if (throwOnMissing)
                {
                    ServiceId id = new(iface, name);
                    ServiceNotFoundException ex = new(string.Format(Resources.Culture, Resources.SERVICE_NOT_FOUND, id.FriendlyName()));
                    ex.Data["path"] = ServicePath.Format
                    (
                        FPath
                            .Select(svc => (IServiceId) svc)
                            .Append(id)
                    );

                    throw ex;
                }

                return null;
            }

            if (Options.StrictDI)
            {
                AbstractServiceEntry? requestor = FPath?.Last;

                //
                // - Ha a fuggosegi fa gyokerenel vagyunk akkor a metodus nem ertelmezett.
                // - A kerelmezett szerviznek legalabb addig kell leteznie mint a kerelmezo szerviznek.
                //

                if (requestor?.Lifetime is not null && requested.Lifetime?.CompareTo(requestor.Lifetime) < 0)
                {
                    var ex = new RequestNotAllowedException(Resources.STRICT_DI);
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

            using (FExclusiveBlock?.Enter())
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
                if (FProvidedDisposables is not null)
                {
                    //
                    // Forditott iranyban szabaditsuk fel a szervizeket mint ahogy letrehoztuk oket (igy az eppen felszabaditas alatt levo szerviz
                    // meg tudja hivatkozni a fuggosegeit).
                    //

                    for (int i = FProvidedDisposables.Count - 1; i >= 0; i--)
                    {
                        ((IDisposable) FProvidedDisposables[i]).Dispose();
                    }
                }

                FExclusiveBlock?.Dispose();
            }

            base.Dispose(disposeManaged);           
        }

        protected override async ValueTask AsyncDispose()
        {
            if (FProvidedDisposables is not null)
            {
                for (int i = FProvidedDisposables.Count - 1; i >= 0; i--)
                {
                    if (FProvidedDisposables[i] is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync();
                    else if (FProvidedDisposables[i] is IDisposable disposable)
                        disposable.Dispose();
                }
            }

            if (FExclusiveBlock is not null)
                await FExclusiveBlock.DisposeAsync();

            //
            // Nem kell "base" hivas mert az a Dispose()-t hivja
            //
        }
        #endregion

        public Injector(ScopeFactory parent) : base(parent)
        {
            if (Options.SafeMode)
                //
                // Feladatabol adodoan nem lehet csak az elso hasznalat elott inicializalni.
                //

                FExclusiveBlock = new ExclusiveBlock(ExclusiveBlockFeatures.SupportsRecursion);
        }

        public new ScopeFactory Parent => (ScopeFactory) base.Parent!;

        #region IInjector
        public object Get(Type iface, string? name) => GetInternal(iface, name, throwOnMissing: true)!;

        public object? TryGet(Type iface, string? name) => GetInternal(iface, name, throwOnMissing: false);

        public ScopeOptions Options => Parent.ScopeOptions;
        #endregion
    }
}
