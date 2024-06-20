/********************************************************************************
* IocComparison.Autofac.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace Solti.Utils.DI.Perf
{
    using Primitives.Patterns;

    public partial class IocComparison
    {
        public sealed class Autofac : Disposable, IIocContainer
        {
            private readonly ContainerBuilder FContainerBuilder = new();

            private IContainer FContainer;

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged)
                    FContainer?.Dispose();

                base.Dispose(disposeManaged);
            }

            public void Build() => FContainer = FContainerBuilder.Build();

            public IDisposable CreateScope(out IServiceProvider provider)
            {
                ILifetimeScope scope = FContainer.BeginLifetimeScope();
                provider = new AutofacServiceProvider(scope);
                return scope;
            }

            public IIocContainer RegisterScoped<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FContainerBuilder.RegisterType<TImplementation>().As<TInterface>().InstancePerLifetimeScope();
                return this;
            }

            public IIocContainer RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FContainerBuilder.RegisterType<TImplementation>().As<TInterface>().SingleInstance();
                return this;
            }

            public IIocContainer RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface
            {
                FContainerBuilder.RegisterType<TImplementation>().As<TInterface>().InstancePerDependency();
                return this;
            }

            public override string ToString() => GetComponentName(typeof(ContainerBuilder));
        }
    }
}
