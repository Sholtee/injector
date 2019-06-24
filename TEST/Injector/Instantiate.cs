/********************************************************************************
* Instantiate.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        private sealed class MyClass
        {
            public IInjector Injector { get; }
            public MyClass(IInjector injector) => Injector = injector;
        }

        [Test]
        public void Injector_Instantiate_ShouldUseTheCurrentInjector()
        {
            new List<IServiceContainer> {Container, Container.CreateChild()}.ForEach(container =>
            {
                using (IInjector injector = container.CreateInjector())
                {
                    MyClass obj = injector.Instantiate<MyClass>();
                    Assert.That(obj.Injector, Is.SameAs(injector));
                }
            });
        }

        [Test]
        public void Injector_Instantiate_ShouldAcceptExplicitArguments()
        {
            var dep = new Implementation_1();

            Implementation_2 obj = Container.CreateInjector().Instantiate<Implementation_2>(new Dictionary<string, object>
            {
                {"interface1", dep}
            });

            Assert.That(obj.Interface1, Is.SameAs(dep));
        }

        [Test]
        public void Injector_Instantiate_ShouldResolveDependencies()
        {
            Container.Service<IInterface_1, Implementation_1>();

            using (IInjector injector = Container.CreateInjector())
            {
                Implementation_2 obj = injector.Instantiate<Implementation_2>();
                Assert.That(obj.Interface1, Is.InstanceOf<Implementation_1>());
            }
        }

        [Test]
        public void Injector_Instantiate_ShouldThrowOnOpenGenericType()
        {
            Assert.Throws<ArgumentException>(() => Container.CreateInjector().Instantiate(typeof(Implementation_3<>)), Resources.CANT_INSTANTIATE_GENERICS);
        }

        [Test]
        public void Injector_Instantiate_ShouldWorkWithClosedGenericTypes()
        {
            Container.Service<IInterface_1, Implementation_1>();

            Implementation_3<string> obj = Container.CreateInjector().Instantiate<Implementation_3<string>>();
            Assert.That(obj.Interface1, Is.InstanceOf<Implementation_1>());
        }

        [Test]
        public void Injector_Instantiate_ShouldTakeIntoAccountTheServiceActivatorAttribute()
        {
            Assert.DoesNotThrow(() => Container.CreateInjector().Instantiate<Implementation_8_multictor>());
        }
    }
}
