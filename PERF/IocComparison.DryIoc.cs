/********************************************************************************
* IocComparison.DryIoc.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using DryIoc;

namespace Solti.Utils.DI.Perf
{
    using Primitives.Patterns;

    public partial class IocComparison
    {
        public sealed class DryIoc : Disposable,  IIocContainer
        {
            private readonly Container FContainer = new();

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    FContainer?.Dispose();

                base.Dispose(disposeManaged);
            }

            public void Build() { }

            public IDisposable CreateScope(out IServiceProvider provider)
            {
                IResolverContext scope = FContainer.OpenScope();
                provider = scope;
                return scope;
            }

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FContainer.Register<TInterface, TImplementation>(reuse: Reuse.Scoped);
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FContainer.Register<TInterface, TImplementation>(reuse: Reuse.Singleton);
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FContainer.Register<TInterface, TImplementation>(reuse: Reuse.Transient);
                return this;
            }

            public override string ToString() => GetComponentName(typeof(Container));
        }
    }
}
