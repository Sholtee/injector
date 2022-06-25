/********************************************************************************
* Diagnostics.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.IO;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Diagnostics;
    using Interfaces;

    public partial class InjectorTests
    {
        [Test]
        public void GetDependencyGraph_ShouldGenerateAValidDotGraph()
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_1, Implementation_1>(Lifetime.Singleton)
                    .Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Scoped)
            );

            string dotGraph = Root.GetDependencyGraph<IInterface_2>(newLine: "\n");
            Assert.That(dotGraph, Is.EqualTo(File.ReadAllText("graph_1.txt")));
        }

        [Test]
        public void GetDependencyGraph_ShouldHandleMissingServices()
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs.Service<IInterface_2, Implementation_2_IInterface_1_Dependant>(Lifetime.Scoped),
                new ScopeOptions
                {
                    ServiceResolutionMode = ServiceResolutionMode.JIT
                }
            );

            string dotGraph = Root.GetDependencyGraph<IInterface_2>(newLine: "\n");
            Assert.That(dotGraph, Is.EqualTo(File.ReadAllText("graph_2.txt")));
        }

        [Test]
        public void GetDependencyGraph_ShouldHandleCircularReference()
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Service<IInterface_4, Implementation_4_CDep>(Lifetime.Scoped)
                    .Service<IInterface_5, Implementation_5_CDep>(Lifetime.Scoped),
                new ScopeOptions
                {
                    ServiceResolutionMode = ServiceResolutionMode.JIT
                }
            );

            string dotGraph = Root.GetDependencyGraph<IInterface_4>(newLine: "\n");
            Assert.That(dotGraph, Is.EqualTo(File.ReadAllText("graph_3.txt")));
        }

        [Test]
        public void GetDependencyGraph_ShouldHandleComplexGraphs()
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Factory<IInterface_1>(injector => (IInterface_1) (object) new { Dep1 = injector.Get<IInterface_2>(null), Dep2 = injector.Get<IInterface_3<int>>(null) }, Lifetime.Singleton)
                    .Factory<IInterface_2>(injector => (IInterface_2) (object) new { Dep1 = injector.TryGet<IInterface_4>(null) /*non-exsiting*/, Dep2 = injector.Get<IInterface_7<object>>("cica") }, Lifetime.Scoped)
                    .Factory(typeof(IInterface_3<>), (_, _) => null, Lifetime.Transient)
                    .Factory(typeof(IInterface_7<>), "cica", (injector, _) => new { Dep1 = injector.Get(typeof(IInterface_1), null) }, Lifetime.Transient),
                new ScopeOptions
                {
                    ServiceResolutionMode = ServiceResolutionMode.JIT
                }
            );

            string dotGraph = Root.GetDependencyGraph<IInterface_1>(newLine: "\n");
            Assert.That(dotGraph, Is.EqualTo(File.ReadAllText("graph_4.txt")));
        }
    }
}
