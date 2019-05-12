using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.Injector.Tests
{
    using Properties;

    [TestFixture]
    public sealed class InjectorTests
    {
        private IInjector Injector;

        [SetUp]
        public void SetupTest()
        {
            Injector = new Injector();
        }

        private interface IInterface_1
        {
        }

        private class Implementation_1 : IInterface_1
        {
        }

        private class DecoratedImplementation_1 : IInterface_1
        {
        }

        private interface IInterface_2
        {
            IInterface_1 Interface1 { get; }
        }

        private class Implementation_2 : IInterface_2
        {
            public Implementation_2(IInterface_1 interface1)
            {
                Interface1 = interface1;
            }

            public IInterface_1 Interface1 { get; }
        }

        private interface IInterface_3<T>
        {
            IInterface_1 Interface1 { get; }
        }

        private class Implementation_3<T> : IInterface_3<T>
        {
            public Implementation_3(IInterface_1 interface1)
            {
                Interface1 = interface1;
            }

            public IInterface_1 Interface1 { get; }
        }

        private interface IInterface_4
        {
        }

        private class Implementation_4 : IInterface_4
        {
            public Implementation_4(IInterface_5 dep)
            {
            }
        }

        private interface IInterface_5
        {
        }

        private class Implementation_5 : IInterface_5
        {
            public Implementation_5(IInterface_4 dep)
            {     
            }
        }

        private interface IInterface_6<T>
        {
            IInterface_3<T> Interface3 { get; }
        }

        private class Implementation_6<T> : IInterface_6<T>
        {
            public Implementation_6(IInterface_3<T> dep)
            {
                Interface3 = dep;
            }

            public IInterface_3<T> Interface3 { get; }
        }

        [TestCase(DependencyType.Transient)]
        [TestCase(DependencyType.Singleton)]
        public void Injector_ShouldInstantiate(object para)
        {
            DependencyType type = (DependencyType) para;

            Injector.Register<IInterface_1, Implementation_1>(type);

            var instance = Injector.Get<IInterface_1>();

            Assert.That(instance, Is.InstanceOf<Implementation_1>());
            (type == DependencyType.Singleton ? Assert.AreSame : ((Action<object, object>) Assert.AreNotSame))(instance, Injector.Get<IInterface_1>());
        }

        [Test]
        public void Injector_ShouldThrowOnNonRegisteredDependency()
        {
            Assert.Throws<NotSupportedException>(() => Injector.Get<IInterface_1>(), string.Format(Resources.DEPENDENCY_NOT_FOUND, typeof(IInterface_1)));         
        }

        [Test]
        public void Injector_ShouldResolveDependencies()
        {
            Injector.Register<IInterface_2, Implementation_2>();
            Injector.Register<IInterface_1, Implementation_1>(); // direkt masodikkent szerepel

            var instance = Injector.Get<IInterface_2>();

            Assert.That(instance, Is.InstanceOf<Implementation_2>());
            Assert.That(instance.Interface1, Is.InstanceOf<Implementation_1>());
        }

        [Test]
        public void Injector_ShouldResolveGenericDependencies()
        {
            Injector.Register<IInterface_1, Implementation_1>();
            Injector.Register(typeof(IInterface_3<>), typeof(Implementation_3<>));
            Injector.Register(typeof(IInterface_6<>), typeof(Implementation_6<>));

            var instance = Injector.Get<IInterface_6<string>>();
            
            Assert.That(instance, Is.InstanceOf<Implementation_6<string>>());
            Assert.That(instance.Interface3, Is.InstanceOf<Implementation_3<string>>());
            Assert.That(instance.Interface3.Interface1, Is.InstanceOf<Implementation_1>());
        }

        [TestCase(DependencyType.Transient)]
        [TestCase(DependencyType.Singleton)]
        public void Injector_ShouldHandleGenericTypes(object para)
        {
            DependencyType type = (DependencyType) para;

            Injector.Register<IInterface_1, Implementation_1>();
            Injector.Register(typeof(IInterface_3<>), typeof(Implementation_3<>), type);

            var instance = Injector.Get<IInterface_3<int>>();

            Assert.That(instance, Is.InstanceOf<Implementation_3<int>>());
            (type == DependencyType.Singleton ? Assert.AreSame : ((Action<object, object>) Assert.AreNotSame))(instance, Injector.Get<IInterface_3<int>>());
        }

        [Test]
        public void Injector_ShouldHandleClosedGenericTypes()
        {
            Injector.Register<IInterface_1, Implementation_1>();
            Injector.Register<IInterface_3<int>, Implementation_3<int>>();

            Assert.That(Injector.Get<IInterface_3<int>>(), Is.InstanceOf<Implementation_3<int>>());
        }

        [Test]
        public void Injector_ShouldNotInstantiateGenericType()
        {
            Injector.Register(typeof(IInterface_3<>), typeof(Implementation_3<>));

            Assert.Throws<InvalidOperationException>(() => Injector.Get(typeof(IInterface_3<>)), Resources.CANT_INSTANTIATE);
        }

        [Test]
        public void Injector_ShouldThrowOnCircularReference()
        {
            Injector.Register<IInterface_4, Implementation_4>();
            Injector.Register<IInterface_5, Implementation_5>();

            Assert.Throws<InvalidOperationException>(() => Injector.Get<IInterface_4>(), string.Join(" -> ", typeof(IInterface_4), typeof(IInterface_5), typeof(IInterface_4)));
            Assert.Throws<InvalidOperationException>(() => Injector.Get<IInterface_5>(), string.Join(" -> ", typeof(IInterface_5), typeof(IInterface_4), typeof(IInterface_5)));
        }

        [Test]
        public void Injector_ShouldResolveItself()
        {
            Assert.AreSame(Injector, Injector.Get<IInjector>());
        }

        [TestCase(DependencyType.Transient)]
        [TestCase(DependencyType.Singleton)]
        public void Injector_ShouldDecorate(object para)
        {
            DependencyType type = (DependencyType) para;

            int
                callbackCallCount = 0,
                typedCallbackCallCount = 0;

            Injector.Register<IInterface_1, Implementation_1>(type)
                .Decorate(inst =>
                {
                    Assert.That(inst, Is.InstanceOf<Implementation_1>());

                    typedCallbackCallCount++;
                    return inst;
                })
                .Decorate((t, inst) =>
                {
                    Assert.That(inst, Is.TypeOf<Implementation_1>());
                    Assert.That(t,    Is.EqualTo(typeof(IInterface_1)));

                    callbackCallCount++;
                    return new DecoratedImplementation_1();
                });

            var instance = Injector.Get<IInterface_1>();
            
            Assert.That(instance, Is.InstanceOf<DecoratedImplementation_1>());
            Assert.That(typedCallbackCallCount, Is.EqualTo(1));
            Assert.That(callbackCallCount, Is.EqualTo(1));

            (type == DependencyType.Singleton ? Assert.AreSame : ((Action<object, object>) Assert.AreNotSame))(instance, Injector.Get<IInterface_1>());
        }

        [Test]
        public void Injector_ShouldThrowOnMultipleConstructors()
        {
            Assert.Throws<NotSupportedException>(
                () => Injector.Register(typeof(IList<>), typeof(List<>)), 
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<>)));

            Assert.Throws<NotSupportedException>(
                () => Injector.Register<IList<int>, List<int>>(),
                string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, typeof(List<int>)));
        }

        [Test]
        public void Injector_ShouldThrowIfTheInterfaceIsNotAssignableFromTheImplementation()
        {
            Assert.Throws<InvalidOperationException>(() => Injector.Register<IInterface_2, Implementation_1>(), string.Format(Resources.NOT_ASSIGNABLE, typeof(IInterface_2), typeof(Implementation_1)));
            Assert.Throws<InvalidOperationException>(() => Injector.Register(typeof(IList<>), typeof(Implementation_1)), string.Format(Resources.NOT_ASSIGNABLE, typeof(IList<>), typeof(Implementation_1)));
            Assert.Throws<InvalidOperationException>(() => Injector.Register<IList<int>, List<string>>(), string.Format(Resources.NOT_ASSIGNABLE, typeof(IList<int>), typeof(List<string>)));
        }
    }
}
