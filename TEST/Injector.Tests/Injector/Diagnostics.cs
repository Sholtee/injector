/********************************************************************************
* Diagnostics.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Diagnostics;
    using Interfaces;
    using Primitives.Patterns;

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
            Assert.That(dotGraph, Is.EqualTo(File.ReadAllText("graph_1.txt").Replace("\r", string.Empty)));
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
            Assert.That(dotGraph, Is.EqualTo(File.ReadAllText("graph_2.txt").Replace("\r", string.Empty)));
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
            Assert.That(dotGraph, Is.EqualTo(File.ReadAllText("graph_3.txt").Replace("\r", string.Empty)));
        }

        [Test]
        public void GetDependencyGraph_ShouldHandleComplexGraphs()
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Factory<IInterface_1>(factoryExpr: injector => (IInterface_1) (object) new { Dep1 = injector.Get<IInterface_2>(null), Dep2 = injector.Get<IInterface_3<int>>(null) }, Lifetime.Singleton)
                    .Factory<IInterface_2>(factoryExpr: injector => (IInterface_2) (object) new { Dep1 = injector.TryGet<IInterface_4>(null) /*non-exsiting*/, Dep2 = injector.Get<IInterface_7<object>>("cica") }, Lifetime.Scoped)
                    .Factory(typeof(IInterface_3<>), factoryExpr: (_, _) => null, Lifetime.Transient)
                    .Factory(typeof(IInterface_7<>), "cica", factoryExpr: (injector, _) => new { Dep1 = injector.Get(typeof(IInterface_1), null) }, Lifetime.Transient),
                new ScopeOptions
                {
                    ServiceResolutionMode = ServiceResolutionMode.JIT
                }
            );

            string dotGraph = Root.GetDependencyGraph<IInterface_1>(newLine: "\n");
            Assert.That(dotGraph, Is.EqualTo(File.ReadAllText("graph_4.txt").Replace("\r", string.Empty)));
        }

        [Test]
        public void GetDependencyGraph_ShouldTakeProxiesIntoAccount()
        {
            Root = ScopeFactory.Create
            (
                svcs => svcs
                    .Factory<IDisposable>(factoryExpr: _ => new Disposable(false), Lifetime.Scoped)
                    .Service<IInterface_1, Implementation_1_No_Dep>(Lifetime.Scoped).Decorate((scope, _, current) => DummyDecorator(scope.Get<IDisposable>(null), current))
            );

            string dotGraph = Root.GetDependencyGraph<IInterface_1>(newLine: "\n");
            Assert.That(dotGraph, Is.EqualTo(File.ReadAllText("graph_5.txt").Replace("\r", string.Empty)));
        }

        private static object DummyDecorator(IDisposable dep, object current) => current;

        [Test]
        public void GetDependencyGraph_ShouldBeNullChecked()
        {
            Root = ScopeFactory.Create(svcs => { });

            Assert.Throws<ArgumentNullException>(() => Root.GetDependencyGraph(null));
            Assert.Throws<ArgumentNullException>(() => IScopeFactoryDiagnosticsExtensions.GetDependencyGraph(null, typeof(IInjector)));
        }

        [Test]
        public void GetDependencyGraph_ShouldThrowOnForeignImplementation()
        {
            Root = new Mock<IScopeFactory>().Object;
            Assert.Throws<NotSupportedException>(() => Root.GetDependencyGraph(typeof(IInjector)));
        }
    }
}
