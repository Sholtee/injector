/********************************************************************************
* IocComparison.MsExtensionsDI.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Microsoft.Extensions.DependencyInjection;

namespace Solti.Utils.DI.Perf
{
    using Primitives.Patterns;

    public partial class IocComparison
    {
        public sealed class MsExtensionsDI : Disposable, IIocContainer
        {
            private readonly IServiceCollection FUnderlyingContainer = new ServiceCollection();

            private ServiceProvider FBuiltProvider;

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    FBuiltProvider?.Dispose();

                base.Dispose(disposeManaged);
            }

            public void Build() => FBuiltProvider = FUnderlyingContainer.BuildServiceProvider();

            public IDisposable CreateScope(out IServiceProvider provider)
            {
                IServiceScope scope = FBuiltProvider.CreateScope();
                provider = scope.ServiceProvider;
                return scope;
            }

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.AddScoped(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.AddSingleton(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FUnderlyingContainer.AddTransient(typeof(TInterface), typeof(TImplementation));
                return this;
            }

            public override string ToString() => nameof(MsExtensionsDI);
        }
    }
}
