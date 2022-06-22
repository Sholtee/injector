﻿/********************************************************************************
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
    }
}
