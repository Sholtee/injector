/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;
    using Properties;

    internal class Injector : ServiceRegistry, IInjector
    {
        #region Private
        private bool FDisposing;

        private readonly ServicePath FPath = new();

        private readonly ExclusiveBlock? FExclusiveBlock;
        #endregion

        #region Internal
        internal IServiceReference GetReferenceInternal(AbstractServiceEntry requested)
        {
            //
            // 1. eset: Csak egy peldanyt kell letrehozni amit vki korabban mar megtett [HasFlag(Built)]
            //          -> visszaadjuk azt.
            //

            if (requested.State.HasFlag(ServiceEntryStates.Built))
            {
                Assert(requested.Instances.Count == 1, "Built entry must contain exactly one service instance");
                return requested.Instances[0];
            }

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
                    {
                        Assert(requested.Instances.Count == 1, "Built entry must contain exactly one service instance");
                        return requested.Instances[0];
                    }

                    //
                    // Letrehozunk egy dedikalt injector-t aminek a felszabaditasa "Parent" feladata lesz
                    //   -> Ennek az injector peldanynak a felszabaditasa nem befolyasolja a szerviz elettartamat.
                    //

                    Injector dedicatedInjector = Parent.CreateScope();

                    try
                    {
                        //
                        // Ugrunk a 3. esetre
                        //

                        return dedicatedInjector.GetReferenceInternal(requested);
                    }
                    catch
                    {
                        dedicatedInjector.Dispose();
                        throw;
                    }
                }
            }

            //
            // 3. eset: Uj peldanyt kell letrehozni 
            //
            // A result.Value itt meg ures, a SetInstance() allitja be
            //

            ServiceReference result = new(requested, this);

            try
            {
                //
                // Az epp letrehozas alatt levo szerviz kerul az ut legvegere igy a fuggosegei feloldasakor o lesz a szulo
                // (FPath.Last).
                //

                FPath.Push(result);
                try
                {
                    if (Options.SafeMode)
                    {
                        //
                        // Ha korabban meg nem peldanyositottuk egyszer sem a szervizt akkor ellenorizzuk h nincs e korkoros referencia
                        // (nyilvan ha korabban mar letre tudtunk hozni peldanyt akkor ez mar felesleges).
                        //

                        if (!FPath.First!.RelatedServiceEntry.State.HasFlag(ServiceEntryStates.Instantiated))
                            FPath.CheckNotCircular();
                    }

                    bool instanceSet = requested.SetInstance(result);
                    Assert(instanceSet && result.Value is not null, "Requested instance could not be set");

                    return result;
                }
                finally
                {
                    FPath.Pop();
                }
            }
            catch
            {
                result.Release(); // NE Dispose() legyen mert azt direktbe nem lehet hivni
                throw;
            }
        }

        internal IServiceReference? GetReferenceInternal(Type iface, string? name, bool throwOnMissing)
        {
            //
            // Ha vkinek a fuggosege vagyunk akkor a fuggo szerviz itt meg nem lehet legyartva.
            //

            Assert(FPath.Last?.Value is null, "Already produced services can not request dependencies");

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
                            .Select(svc => (IServiceId) svc.RelatedServiceEntry)
                            .Append(id)
                    );

                    throw ex;
                }

                return null;
            }

            if (Options.StrictDI)
            {
                AbstractServiceEntry? requestor = FPath.Last?.RelatedServiceEntry;

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

            IServiceReference resolved = GetReferenceInternal(requested);

            //
            // Minden fuggoseget megtalaltunk, a szerviz sikeresen peldanyositasra kerult.
            // Ha a szerviz egy masik szerviz fuggosege akkor felvesszuk annak fuggosegi listajaba.
            //

            FPath.Last?.AddDependency(resolved);
            return resolved;
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
            base.Dispose(disposeManaged);

            //
            // Mivel felszabaditaskor elmeletben meg kerdezhetunk le szervizeket (szerviz Dispose()
            // metodusaban van injector.Get() hivas) ezert az exkluziv blokkot csak a base() hivas
            // utan szabaditsuk fel.
            //

            if (disposeManaged)
                FExclusiveBlock?.Dispose();
        }

        protected override async ValueTask AsyncDispose()
        {
            await base.AsyncDispose();

            if (FExclusiveBlock is not null)
                await FExclusiveBlock.DisposeAsync();
        }
        #endregion

        public Injector(ScopeFactory parent) : base(parent)
        {
            if (Options.SafeMode)
                FExclusiveBlock = new ExclusiveBlock(ExclusiveBlockFeatures.SupportsRecursion);
        }

        public new ScopeFactory Parent => (ScopeFactory) base.Parent!;

        #region IInjector
        public IServiceReference GetReference(Type iface, string? name)
        {
            CheckNotDisposed();

            if (FDisposing)
                throw new InvalidOperationException(Resources.INJECTOR_IS_BEING_DISPOSED);

            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            using (FExclusiveBlock?.Enter())
            {
                return GetReferenceInternal(iface, name, throwOnMissing: true)!;
            }
        }

        public IServiceReference? TryGetReference(Type iface, string? name)
        {
            CheckNotDisposed();

            if (FDisposing)
                throw new InvalidOperationException(Resources.INJECTOR_IS_BEING_DISPOSED);

            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            using (FExclusiveBlock?.Enter())
            {
                return GetReferenceInternal(iface, name, throwOnMissing: false);
            }
        }

        public object Get(Type iface, string? name) => GetReference(iface, name).GetInstance()!;

        public object? TryGet(Type iface, string? name) => TryGetReference(iface, name)?.GetInstance();

        public ScopeOptions Options => Parent.ScopeOptions;
        #endregion
    }
}
