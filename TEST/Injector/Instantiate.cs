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
            MyClass obj = Injector.Instantiate<MyClass>();
            Assert.That(obj.Injector, Is.SameAs(Injector));

            using (IInjector child = Injector.CreateChild())
            {
                obj = child.Instantiate<MyClass>();
                Assert.That(obj.Injector, Is.SameAs(child));
            }
        }

        [Test]
        public void Injector_Instantiate_ShouldAcceptExplicitArguments()
        {
            var dep = new Implementation_1();

            Implementation_2 obj = Injector.Instantiate<Implementation_2>(new Dictionary<string, object>
            {
                {"interface1", dep}
            });

            Assert.That(obj.Interface1, Is.SameAs(dep));
        }

        [Test]
        public void Injector_Instantiate_ShouldThrowOnOpenGenericType()
        {
            Assert.Throws<ArgumentException>(() => Injector.Instantiate(typeof(Implementation_3<>)), Resources.CANT_INSTANTIATE_GENERICS);
        }

        [Test]
        public void Injector_Instantiate_ShouldWorkWithClosedGenericTypes()
        {
            Injector.Service<IInterface_1, Implementation_1>();

            Implementation_3<string> obj = Injector.Instantiate<Implementation_3<string>>();
            Assert.That(obj.Interface1, Is.InstanceOf<Implementation_1>());
        }

        [Test]
        public void Injector_Instantiate_ShouldTakeIntoAccountTheServiceActivatorAttribute()
        {
            Assert.DoesNotThrow(() => Injector.Instantiate<Implementation_8_multictor>());
        }
    }
}
