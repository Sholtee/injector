/********************************************************************************
* IocComparison.Lamar.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Lamar;

namespace Solti.Utils.DI.Perf
{
    using Primitives.Patterns;

    public partial class IocComparison
    {
        public sealed class Lamar : Disposable,  IIocContainer
        {
            private Container FBuiltContainer;

            private readonly ServiceRegistry FRegistry = new();

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    FBuiltContainer?.Dispose();

                base.Dispose(disposeManaged);
            }

            public void Build() => FBuiltContainer = new Container(FRegistry);

            public IDisposable CreateScope(out IServiceProvider provider)
            {
                INestedContainer nestedContainer = FBuiltContainer.GetNestedContainer();
                provider = nestedContainer;
                return nestedContainer;
            }

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FRegistry
                    .For(typeof(TInterface))
                    .Use(typeof(TImplementation))
                    .Scoped();
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FRegistry
                    .For(typeof(TInterface))
                    .Use(typeof(TImplementation))
                    .Singleton();
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FRegistry
                    .For(typeof(TInterface))
                    .Use(typeof(TImplementation))
                    .Transient();
                return this;
            }

            public override string ToString() => nameof(Lamar);
        }
    }
}
