/********************************************************************************
* IocComparison.InjectorDotNet.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Primitives.Patterns;

    public partial class IocComparison
    {
        public sealed class InjectorDotNet : Disposable, IIocContainer
        {
            private IScopeFactory FRoot;

            private readonly ServiceCollection FServices = new();

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    FRoot?.Dispose();        

                base.Dispose(disposeManaged);
            }

            public void Build() => FRoot = ScopeFactory.Create
            (
                FServices,
                new ScopeOptions 
                {
                    MaxSpawnedTransientServices = int.MaxValue,
                    SupportsServiceProvider = true
                }
            );

            public IDisposable CreateScope(out IServiceProvider provider) => FRoot.CreateScope(out provider);

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FServices.Service(typeof(TInterface), typeof(TImplementation), Lifetime.Scoped);
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FServices.Service(typeof(TInterface), typeof(TImplementation), Lifetime.Singleton);
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FServices.Service(typeof(TInterface), typeof(TImplementation), Lifetime.Transient);
                return this;
            }

            public override string ToString() => nameof(InjectorDotNet);
        }
    }
}
