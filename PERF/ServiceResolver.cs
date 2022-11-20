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
        public interface IMyService { }

        public class MyService : IMyService { }

        public static IEnumerable<Lifetime> Lifetimes
        {
            get
            {
                yield return DI.Lifetime.Transient;
                yield return DI.Lifetime.Scoped;
                yield return DI.Lifetime.Singleton;
                yield return DI.Lifetime.Pooled;
            }
        }

        [ParamsSource(nameof(Lifetimes))]
        public LifetimeBase Lifetime { get; set; }

        private ResolveServiceDelegate ResolveImpl { get; set; }

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
