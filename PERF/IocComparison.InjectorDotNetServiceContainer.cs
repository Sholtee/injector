/********************************************************************************
* IocComparison.InjectorDotNetServiceContainer.cs                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Primitives.Patterns;

    public partial class IocComparison
    {
        public sealed class InjectorDotNetServiceContainer : Disposable, IIocContainer
        {
            private IScopeFactory FRoot;

            private readonly List<Action<IServiceCollection>> FRegistrationActions = new();

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    FRoot?.Dispose();        

                base.Dispose(disposeManaged);
            }

            public void Build() => FRoot = ScopeFactory.Create
            (
                svcs => FRegistrationActions.ForEach(addRegistration => addRegistration(svcs)),
                new ScopeOptions 
                {
                    MaxSpawnedTransientServices = int.MaxValue,
                    SafeMode = false,
                    SupportsServiceProvider = true
                }
            );

            public IDisposable CreateScope(out IServiceProvider provider) => FRoot.CreateScope(out provider);

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FRegistrationActions.Add(svcs => svcs.Service(typeof(TInterface), typeof(TImplementation), Lifetime.Scoped));
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FRegistrationActions.Add(svcs => svcs.Service(typeof(TInterface), typeof(TImplementation), Lifetime.Singleton));
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FRegistrationActions.Add(svcs => svcs.Service(typeof(TInterface), typeof(TImplementation), Lifetime.Transient));
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface>(Func<IServiceProvider, TInterface> factory) where TInterface : class
            {
                FRegistrationActions.Add(svcs => svcs.Factory(injector => factory((IServiceProvider) injector), Lifetime.Singleton));
                return this;
            }

            public IIocContainer RegisterScoped<TInterface>(Func<IServiceProvider, TInterface> factory) where TInterface : class
            {
                FRegistrationActions.Add(svcs => svcs.Factory(injector => factory((IServiceProvider) injector), Lifetime.Scoped));
                return this;
            }

            public IIocContainer RegisterTransient<TInterface>(Func<IServiceProvider, TInterface> factory) where TInterface : class
            {
                FRegistrationActions.Add(svcs => svcs.Factory(injector => factory((IServiceProvider) injector), Lifetime.Transient));
                return this;
            }

            public override string ToString() => nameof(InjectorDotNetServiceContainer);
        }
    }
}
