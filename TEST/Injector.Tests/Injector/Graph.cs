/********************************************************************************
* Graph.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Graph.Tests
{
    using Diagnostics;
    using Interfaces;
    using Internals;

    [TestFixture]
    public class GraphTests
    {
        private static IServiceReference[] Validate(IInjector injector) 
        {
            IServiceReference
                svc1, svc2, svc3, svc4;

            svc4 = injector.GetReference(typeof(IInterface_4), null);
            Assert.That(svc4.RefCount, Is.EqualTo(1));
            Assert.That(svc4.Dependencies.Count, Is.EqualTo(3));
            Assert.NotNull(GetDependency(svc4, typeof(IInjector)));
            Assert.NotNull(GetDependency(svc4, typeof(IInterface_2)));
            Assert.NotNull(GetDependency(svc4, typeof(IInterface_3)));

            svc3 = GetDependency(svc4, typeof(IInterface_3));
            Assert.That(svc3.RefCount, Is.EqualTo(2));
            Assert.That(svc3.Dependencies.Count, Is.EqualTo(2));
            Assert.NotNull(GetDependency(svc3, typeof(IInterface_1)));
            Assert.NotNull(GetDependency(svc3, typeof(IInterface_2)));

            svc2 = GetDependency(svc4, typeof(IInterface_2));
            Assert.That(svc2.RefCount, Is.EqualTo(3));
            Assert.That(svc2.Dependencies.Count, Is.EqualTo(1));
            Assert.NotNull(GetDependency(svc2, typeof(IInterface_1)));

            svc1 = GetDependency(svc3, typeof(IInterface_1));
            Assert.That(svc1.RefCount, Is.EqualTo(2));
            Assert.That(svc1.Dependencies.Count, Is.EqualTo(0));

            return new[] { svc1, svc2, svc3, svc4 };

            IServiceReference GetDependency(IServiceReference reference, Type iface, string name = null) => reference.Dependencies.SingleOrDefault(dep => dep.RelatedServiceEntry.Interface == iface && dep.RelatedServiceEntry.Name == name);
        }

        [Test]
        public void ComplexTest()
        {
            IServiceReference[] references;

            using (IScopeFactory root = DI.ScopeFactory.Create(svcs => svcs
                    .Service<IInterface_1, Implementation_1>(Lifetime.Transient)
                    .Service<IInterface_2, Implementation_2>(Lifetime.Singleton)
                    .Service<IInterface_3, Implementation_3>(Lifetime.Transient)
                    .Service<IInterface_4, Implementation_4>(Lifetime.Scoped)))
            { 
                references = Validate(root.CreateScope());
            }

            Assert.That(references.All(reference => reference.RefCount == 0));
        }

        [Test]
        public void DotGraphTest()
        {
            using IScopeFactory root = DI.ScopeFactory.Create(svcs => svcs
                .Service<IInterface_1, Implementation_1>(Lifetime.Transient)
                .Service<IInterface_2, Implementation_2>(Lifetime.Singleton)
                .Service<IInterface_3, Implementation_3>(Lifetime.Transient)
                .Service<IInterface_4, Implementation_4>(Lifetime.Scoped));

            IInjector injector = root.CreateScope(); // nem kell using, root.Dispose() ot is likvidalja

            string 
                dotGraph = injector.GetDependencyGraph<IInterface_4>(),             
                id1 = ContainsNode("<<u>Solti.Utils.DI.Injector.Graph.Tests.GraphTests.IInterface_4</u><br/><br/><i>Scoped</i>>"),
                id2 = ContainsNode("<<u>Solti.Utils.DI.Interfaces.IInjector</u><br/><br/><i>NULL</i>>"),
                id3 = ContainsNode("<<u>Solti.Utils.DI.Injector.Graph.Tests.GraphTests.IInterface_2</u><br/><br/><i>Singleton</i>>"),
                id4 = ContainsNode("<<u>Solti.Utils.DI.Injector.Graph.Tests.GraphTests.IInterface_1</u><br/><br/><i>Transient</i>>"),
                id5 = ContainsNode("<<u>Solti.Utils.DI.Injector.Graph.Tests.GraphTests.IInterface_3</u><br/><br/><i>Transient</i>>");

            ContainsEdge(id1, id2);
            ContainsEdge(id1, id3);
            ContainsEdge(id3, id4);
            ContainsEdge(id1, id5);
            ContainsEdge(id5, id4);
            ContainsEdge(id5, id3);

            string ContainsNode(string str)
            {
                str = Regex.Replace(str, "\\.|\\/|\\$", match => $"\\{match.Value}");

                Match match = Regex.Match(dotGraph, $"  (?<id>N_[0-9A-F]{{8}}) \\[shape=box,margin=\\.1,label={str}\\];", RegexOptions.Multiline);

                Assert.That(match.Captures, Has.Count.EqualTo(1));

                return match.Groups["id"].Value;
            }

            void ContainsEdge(string id1, string id2)
            {
                Assert.That(Regex.Match(dotGraph, $"{id1} -> {id2}", RegexOptions.Multiline).Captures, Has.Count.EqualTo(1));
            }
        }

        private interface IInterface_1 { }
        private interface IInterface_2 { }
        private interface IInterface_3 { }
        private interface IInterface_4 { }

        private class Implementation_1 : IInterface_1 { }

        private class Implementation_2 : IInterface_2 
        {
            public Implementation_2(IInterface_1 dep) { }
        }

        private class Implementation_3 : IInterface_3 
        {
            public Implementation_3(IInterface_1 dep1, IInterface_2 dep2) { }
        }

        private class Implementation_4 : IInterface_4 
        {
            public Implementation_4(IInjector injector) 
            {
                injector.Get<IInterface_2>();
                injector.Get<IInterface_3>();              
            }
        }
    }
}
