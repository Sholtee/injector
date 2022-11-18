/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace Solti.Utils.DI.Perf
{
    using Interfaces;
    using Internals;

    public class ServiceResolver
    {
        static ServiceResolver() => InjectorDotNetLifetime.Initialize();

        public interface IMyService { }

        public class MyService : IMyService { }

        public static IEnumerable<Lifetime> Lifetimes
        {
            get
            {
                yield return Lifetime.Transient;
                yield return Lifetime.Scoped;
                yield return Lifetime.Singleton;
                yield return Lifetime.Pooled;
            }
        }

        [ParamsSource(nameof(Lifetimes))]
        public Lifetime Lifetime { get; set; }

        private ResolveDelegate ResolveImpl { get; set; }

        private IInstanceFactory Scope { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Injector 
                root = new(Lifetime.CreateFrom(typeof(IMyService), null, typeof(MyService)), new ScopeOptions(), null),
                scope = new(root, null);

            ResolveImpl = scope.ServiceLookup.Get(typeof(IMyService), null).ResolveInstance;

            Scope = scope;
        }

        [Benchmark]
        public object Resolve() => ResolveImpl(Scope);
    }
}
