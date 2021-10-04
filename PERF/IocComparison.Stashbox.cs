﻿/********************************************************************************
* IocComparison.Stashbox.cs                                                     *
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
        public sealed class Stashbox : Disposable, IIocContainer
        {
            private readonly IServiceCollection FUnderlyingContainer = new ServiceCollection();

            private IServiceProvider FBuiltProvider;

            public void Build() => FBuiltProvider = FUnderlyingContainer.UseStashbox();

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

            public IIocContainer RegisterSingleton<TInterface>(Func<IServiceProvider, TInterface> factory) where TInterface : class
            {
                FUnderlyingContainer.AddSingleton(typeof(TInterface), factory);
                return this;
            }

            public IIocContainer RegisterScoped<TInterface>(Func<IServiceProvider, TInterface> factory) where TInterface : class
            {
                FUnderlyingContainer.AddScoped(typeof(TInterface), factory);
                return this;
            }

            public IIocContainer RegisterTransient<TInterface>(Func<IServiceProvider, TInterface> factory) where TInterface : class
            {
                FUnderlyingContainer.AddTransient(typeof(TInterface), factory);
                return this;
            }

            public override string ToString() => nameof(Stashbox);
        }
    }
}
