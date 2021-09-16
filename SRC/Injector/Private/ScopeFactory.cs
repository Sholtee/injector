/********************************************************************************
* ScopeFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;

    internal class ScopeFactory : ConcurrentServiceRegistry, IScopeFactory
    {
        //
        // A "rendszer" scope-okat a rendszer hpeldanyositja megosztott szervizek letrehozasakor ezert tovabb kell
        // letezzenek mint a "user" scope-ok.
        //

        private readonly ConcurrentScopeCollection FSystemScopes = new();

        private readonly ConcurrentScopeCollection FUserScopes = new();

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
                ReversedDispose(FUserScopes);

                //
                // "system" scope-okat a letrehozasuk forditott sorrendjeben szabaditjuk fel a "user"
                // scope-ok utan,
                //

                ReversedDispose(FSystemScopes);
            }

            base.Dispose(disposeManaged);

            static void ReversedDispose(ConcurrentScopeCollection scopes)
            {
                while (scopes.TakeFirst(out Injector scope)) // AddFirst() adta hozza -> forditott a sorrend
                {
                    scope.Dispose();
                }
            }
        }

        protected async override ValueTask AsyncDispose()
        {
            await ReversedDispose(FUserScopes);
            await ReversedDispose(FSystemScopes);
            await base.AsyncDispose();

            static async Task ReversedDispose(ConcurrentScopeCollection scopes)
            {
                while (scopes.TakeFirst(out Injector scope))
                {
                    await scope.DisposeAsync();
                }
            }
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

        protected virtual Injector CreateScopeInternal() => new Injector(this);

        public IInjector CreateScope()
        {
            Injector scope = CreateScopeInternal();
            if (ScopeOptions.SafeMode)
                FUserScopes.Add(scope);
            return scope;
        }

        public IInjector CreateSystemScope()
        {
            Injector scope = CreateScopeInternal();
            FSystemScopes.Add(scope); // minden kepp hozzaadjuk (a "user" tuti nem szabaditja fel)
            return scope;
        }

        public ScopeFactory(ISet<AbstractServiceEntry> entries, ScopeOptions scopeOptions, CancellationToken cancellation = default) : base(entries, cancellation: cancellation)
        {
            ScopeOptions = scopeOptions;
        }

        public ScopeOptions ScopeOptions { get; }

        public IReadOnlyCollection<IInjector> SystemScopes => FSystemScopes;

        public IReadOnlyCollection<IInjector> UserScopes => FUserScopes;
    }
}
