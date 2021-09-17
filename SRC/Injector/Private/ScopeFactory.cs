/********************************************************************************
* ScopeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;

    internal class ScopeFactory : ConcurrentServiceRegistry, IScopeFactory, ICaptureDisposable
    {
        private readonly ConcurrentScopeCollection FScopes = new();

        //
        // A rendszer scope-ok altal kozze tett szervizek egyszerre tobb scope-ban is hasznalva lehetnek (AbstractServiceEntry.IsShared)
        // ezert azok felszabaditasat kulon vegezzuk, letrehozasuk forditott sorrendjeben (igy az eppen felszabaditas alatt levo szerviz
        // meg tudja hivatkozni a fuggosegeit).
        //

        private readonly ConcurrentStack<object> FCapturedDisposables = new();

        private sealed class ConcurrentScopeCollection : ConcurrentLinkedList<Injector>, IReadOnlyCollection<Injector>
        {
            public void Add(Injector item)
            {
                LinkedListNode<Injector> node = AddFirst(item);
                item.OnDispose += (_, _) =>
                {
                    //
                    // A Dispose() karakterisztikajabol adodoan ez a metodus biztosan csak egyszer lesz meghivva
                    //

                    if (node.Owner is not null) // Takefirst() mar kivehette a listabol
                        Remove(node);
                };
            }
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                while (FScopes.TakeFirst(out Injector scope))
                {
                    scope.Dispose();
                }

                // Debug.Assert(FScopes.Count == 0, "Scope block must be empty");

                while (FCapturedDisposables.TryPop(out object obj))
                {
                    switch (obj)
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

        protected async override ValueTask AsyncDispose()
        {
            while (FScopes.TakeFirst(out Injector scope))
            {
                await scope.DisposeAsync();
            }

            // Debug.Assert(FScopes.Count == 0, "Scope block must be empty");

            while (FCapturedDisposables.TryPop(out object obj))
            {
                switch (obj)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync();
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }

            await base.AsyncDispose();
        }

        protected static IReadOnlyCollection<AbstractServiceEntry> DefaultBuiltInServices { get; } = new AbstractServiceEntry[]
        {
            new ContextualServiceEntry(typeof(IServiceRegistry), null, owner => owner),
            new ContextualServiceEntry(typeof(IInjector), null,  owner => (IInjector) owner),
            new ContextualServiceEntry(typeof(IScopeFactory), null, owner => (IScopeFactory) owner.Parent!),
            new ContextualServiceEntry(typeof(IDictionary<string, object?>), IInjectorBasicExtensions.META_NAME, _ => new Dictionary<string, object?>()), // ne Scoped legyen h StrictDI ne anyazzon
            new ScopedServiceEntry(typeof(IEnumerable<>), null, typeof(ServiceEnumerator<>), null!)
        };

        protected override IReadOnlyCollection<AbstractServiceEntry> BuiltInServices { get; } = DefaultBuiltInServices;

        protected virtual Injector CreateScopeInternal(ScopeKind kind) => new Injector(this, kind);

        public IInjector CreateScope()
        {
            Injector scope = CreateScopeInternal(ScopeKind.User);
            if (ScopeOptions.SafeMode)
                FScopes.Add(scope);
            return scope;
        }

        public IInjector CreateSystemScope()
        {
            Injector scope = CreateScopeInternal(ScopeKind.System);
            FScopes.Add(scope); // minden kepp hozzaadjuk (a "user" tuti nem szabaditja fel)
            return scope;
        }

        public ScopeFactory(ISet<AbstractServiceEntry> entries, ScopeOptions scopeOptions, CancellationToken cancellation = default) : base(entries, cancellation: cancellation)
        {
            ScopeOptions = scopeOptions;
        }

        public void CaptureDisposable(object obj) => FCapturedDisposables.Push(obj);

        public ScopeOptions ScopeOptions { get; }

        public IReadOnlyCollection<IInjector> Scopes => FScopes;
    }
}
