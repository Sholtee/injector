/********************************************************************************
* IocComparison.Stashbox.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Stashbox;
using Stashbox.Extensions.DependencyInjection;
using Stashbox.Lifetime;

namespace Solti.Utils.DI.Perf
{
    using Primitives.Patterns;

    public partial class IocComparison
    {
        public sealed class Stashbox : Disposable, IIocContainer
        {
            private readonly StashboxContainer FUnderlyingContainer = new(opts => opts.WithDisposableTransientTracking());

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    FUnderlyingContainer.Dispose();

                base.Dispose(disposeManaged);
            }

            public void Build() { }

            public IDisposable CreateScope(out IServiceProvider provider)
            {
                IDependencyResolver scope = FUnderlyingContainer.BeginScope();
                provider = new StashboxServiceProvider(scope);
                return scope;
            }

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.Register(typeof(TInterface), typeof(TImplementation), opts => opts.WithLifetime(Lifetimes.Scoped));
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.Register(typeof(TInterface), typeof(TImplementation), opts => opts.WithLifetime(Lifetimes.Singleton));
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.Register(typeof(TInterface), typeof(TImplementation), opts => opts.WithLifetime(Lifetimes.Transient));
                return this;
            }

            public override string ToString() => nameof(Stashbox);
        }
    }
}
