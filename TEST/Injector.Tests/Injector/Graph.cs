/********************************************************************************
* Graph.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Graph.Tests
{
    using Interfaces;
    using Internals;

    [TestFixture]
    public class GraphTests
    {
        private static IServiceReference[] Validate(Injector injector) 
        {
            Config.Value.Injector.StrictDI = false;

            IServiceReference
                svc1, svc2, svc3, svc4;

            svc4 = injector.GetReference(typeof(IInterface_4), null);
            Assert.That(svc4.RefCount, Is.EqualTo(1));
            Assert.That(svc4.Dependencies.Count(), Is.EqualTo(3));
            Assert.NotNull(GetDependency(svc4, typeof(IInjector)));
            Assert.NotNull(GetDependency(svc4, typeof(IInterface_2)));
            Assert.NotNull(GetDependency(svc4, typeof(IInterface_3)));

            svc3 = GetDependency(svc4, typeof(IInterface_3));
            Assert.That(svc3.RefCount, Is.EqualTo(2));
            Assert.That(svc3.Dependencies.Count(), Is.EqualTo(2));
            Assert.NotNull(GetDependency(svc3, typeof(IInterface_1)));
            Assert.NotNull(GetDependency(svc3, typeof(IInterface_2)));

            svc2 = GetDependency(svc4, typeof(IInterface_2));
            Assert.That(svc2.RefCount, Is.EqualTo(3));
            Assert.That(svc2.Dependencies.Count(), Is.EqualTo(1));
            Assert.NotNull(GetDependency(svc2, typeof(IInterface_1)));

            svc1 = GetDependency(svc3, typeof(IInterface_1));
            Assert.That(svc1.RefCount, Is.EqualTo(2));
            Assert.That(svc1.Dependencies.Count(), Is.EqualTo(0));

            return new[] { svc1, svc2, svc3, svc4 };

            IServiceReference GetDependency(IServiceReference reference, Type iface) => reference.Dependencies.SingleOrDefault(dep => dep.RelatedServiceEntry.Interface == iface);
        }

        [Test]
        public void ComplexTest()
        {
            IServiceReference[] references;

            using (IServiceContainer container = new ServiceContainer())
            {
                container
                    .Service<IInterface_1, Implementation_1>(Lifetime.Transient)
                    .Service<IInterface_2, Implementation_2>(Lifetime.Singleton)
                    .Service<IInterface_3, Implementation_3>(Lifetime.Transient)
                    .Service<IInterface_4, Implementation_4>(Lifetime.Scoped);

                references = Validate(new Injector(container));
            }

            Assert.That(references.All(reference => reference.RefCount == 0));
        }

        [Test]
        public void ComplexTestWithChildContainer()
        {
            IServiceReference[] references;

            using (IServiceContainer container = new ServiceContainer())
            {
                container
                    .Service<IInterface_1, Implementation_1>(Lifetime.Transient)
                    .Service<IInterface_2, Implementation_2>(Lifetime.Singleton);

                IServiceContainer child = container.CreateChild();
                child
                    .Service<IInterface_3, Implementation_3>(Lifetime.Transient)
                    .Service<IInterface_4, Implementation_4>(Lifetime.Scoped);

                references = Validate(new Injector(child));
            }

            Assert.That(references.All(reference => reference.RefCount == 0));
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
