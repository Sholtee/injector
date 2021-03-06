﻿/********************************************************************************
* Resolver.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;
    using Primitives.Patterns;
    using Properties;
    using Proxy;
    using Proxy.Generators;

    [TestFixture]
    public sealed class ResolverTests
    {
        private class MyClass
        {
            public IDisposable Dep1 { get; }
            public IList Dep2 { get; }
            public int Int { get; }

            public MyClass(IDisposable dep1, [Options(Name = "cica")] IList dep2)
            {
                Dep1 = dep1;
                Dep2 = dep2;
            }

            public MyClass(IDisposable dep1, IList dep2, int @int)
            {
                Dep1 = dep1;
                Dep2 = dep2;
                Int  = @int;
            }

            public MyClass([Options(Name = "cica")] Lazy<IDisposable> dep1, Lazy<IList> dep2)
            {
                Dep1 = dep1.Value;
                Dep2 = dep2.Value;
            }

            public MyClass()
            {
            }
        }

        [TestCase(typeof(IDisposable), null, typeof(IList), "cica")]
        [TestCase(typeof(Lazy<IDisposable>), "cica", typeof(Lazy<IList>), null)]
        public void Factory_ShouldResolveDependencies(Type dep1, string name1, Type dep2, string name2)
        {
            var mockDisposable = new Mock<IDisposable>();

            var mockServiceFactory = new Mock<IList>();

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            foreach(Func<IInjector, object> resolver in GetResolvers(typeof(MyClass).GetConstructor(new[] { dep1, dep2 })))
            { 
                mockInjector
                    .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                    .Returns<Type, string>((type, name) =>
                    {
                        if (type == typeof(IDisposable) && name == name1) return mockDisposable.Object;
                        if (type == typeof(IList) && name == name2) return mockServiceFactory.Object;

                        Assert.Fail("Unknown type");
                        return null;
                    });
                   
                MyClass instance = (MyClass) resolver(mockInjector.Object);

                Assert.That(instance, Is.Not.Null);
                Assert.That(instance.Dep1, Is.SameAs(mockDisposable.Object));
                Assert.That(instance.Dep2, Is.SameAs(mockServiceFactory.Object));
            
                mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), It.Is<string>(n => n == name1)), Times.Once);
                mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IList)), It.Is<string>(n => n == name2)), Times.Once);
                mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Exactly(2));

                mockInjector.Reset();
            }

            IEnumerable<Func<IInjector, object>> GetResolvers(ConstructorInfo ctor)
            {
                Func<IInjector, Type, object> factory = Resolver.Get(ctor);
                yield return injector => factory(injector, null);

                Func<IInjector, IReadOnlyDictionary<string, object>, object> factoryEx = Resolver.GetExtended(ctor);
                yield return injector => factoryEx(injector, new Dictionary<string, object>(0));
            }
        }

        [Test]
        public void Get_ShouldHandleParameterlessConstructors()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()));

            Func<IInjector, Type, object> factory = Resolver
                .Get(typeof(MyClass).GetConstructor(new Type[0]));

            Assert.That(factory, Is.Not.Null);

            MyClass instance = (MyClass) factory(mockInjector.Object, null);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.Null);
            Assert.That(instance.Dep2, Is.Null);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExtendedFactory_ShouldHandleExplicitArguments()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()));

            Func<IInjector, IReadOnlyDictionary<string, object>, object> factory = Resolver
                .GetExtended(typeof(List<string>).GetConstructor(new[] {typeof(int)}));

            object lst = factory(mockInjector.Object, new Dictionary<string, object>
            {
                {"capacity", 10}
            });

            Assert.That(lst, Is.InstanceOf<List<string>>());
            Assert.That(((List<string>) lst).Capacity, Is.EqualTo(10));
           
            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExtendedFactory_ShouldHandleOptionalArguments() 
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<IDictionary>), null))
                .Returns(new List<IDictionary>());
            mockInjector
                .Setup(i => i.TryGet(typeof(IDisposable), null))
                .Returns(null);

            Func<IInjector, IReadOnlyDictionary<string, object>, object> factory = Resolver
                .GetExtended(typeof(MyInterceptor));

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, new Dictionary<string, object>()));

            mockInjector.Verify(i => i.Get(typeof(IList<IDictionary>), null), Times.Once);
            mockInjector.Verify(i => i.TryGet(typeof(IDisposable), null), Times.Once);
        }

        [Test]
        public void ExtendedFactory_ShouldThrowOnNonInterfaceArguments()
        {
            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList), typeof(int) });

            //
            // Sima Get() hivas nem fog mukodni (nem interface parameter).
            //

            Assert.Throws<ArgumentException>(() => Resolver.Get(ctor), Resources.INVALID_CONSTRUCTOR);

            //
            // Ne mock-oljuk az injector-t h a megfelelo kiveteleket kapjuk
            //

            using (IServiceContainer container = new ServiceContainer())
            {
                IInjector injector = container
                    .Factory<IDisposable>(i => new Disposable(), Lifetime.Scoped)
                    .Factory<IList>(i => new List<object>(), Lifetime.Scoped)
                    .CreateInjector();

                //
                // Ez mukodne viszont nem adtuk meg a nem interface parametert.
                //

                Assert.Throws<ArgumentException>(() => Resolver.GetExtended(ctor).Invoke(injector, new Dictionary<string, object>(0)), Resources.PARAMETER_NOT_AN_INTERFACE);
            }
        }

        [Test]
        public void Get_ShouldValidate() 
        {
            Assert.Throws<ArgumentException>(() => Resolver.Get(typeof(IDisposable)), Resources.PARAMETER_NOT_A_CLASS);
            Assert.Throws<ArgumentException>(() => Resolver.Get(typeof(IList<>)), Resources.PARAMETER_IS_GENERIC);
            Assert.Throws<ArgumentException>(() => Resolver.Get(typeof(AbstractClass)), Resources.PARAMETER_IS_ABSTRACT);
        }

        [Test]
        public void GetExtended_ShouldValidate()
        {
            Assert.Throws<ArgumentException>(() => Resolver.GetExtended(typeof(IDisposable)), Resources.PARAMETER_NOT_A_CLASS);
            Assert.Throws<ArgumentException>(() => Resolver.GetExtended(typeof(IList<>)), Resources.PARAMETER_IS_GENERIC);
            Assert.Throws<ArgumentException>(() => Resolver.GetExtended(typeof(AbstractClass)), Resources.PARAMETER_IS_ABSTRACT);
        }

        private abstract class AbstractClass { }

        [Test]
        public void ExtendedFactory_ExplicitArgumentsShouldSuppressTheInjectorInvocation()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => null);

            Func<IInjector, IReadOnlyDictionary<string, object>, object> factory = Resolver
                .GetExtended(typeof(MyClass).GetConstructor(new[] {typeof(IDisposable), typeof(IList) }));

            object obj = factory(mockInjector.Object, new Dictionary<string, object>
            {
                {"dep2", null}
            });

            Assert.That(obj, Is.InstanceOf<MyClass>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IList)), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExtendedFactory_ExplicitArgumentsMayContainNonInterfaceValues()
        {
            const int TEN = 10;

            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList), typeof(int) });

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => null);

            MyClass obj = Resolver.GetExtended(ctor).Invoke(mockInjector.Object, new Dictionary<string, object> { { "int", TEN } }) as MyClass;

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.Int, Is.EqualTo(TEN));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void GetLazyFactory_ShouldReturnProperFactory(string svcName)
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => new Disposable());

            Func<IInjector, object> factory = Resolver.GetLazyFactory(typeof(IDisposable), new OptionsAttribute { Name = svcName });
            Assert.That(factory, Is.Not.Null);

            Lazy<IDisposable> lazy = factory(mockInjector.Object) as Lazy<IDisposable>;
            Assert.That(lazy, Is.Not.Null);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);

            IDisposable retval = lazy.Value;
            Assert.That(retval, Is.InstanceOf<Disposable>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), It.Is<string>(s => s == svcName)), Times.Once);
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void GetLazyFactory_ShouldCache(string svcName)
        {
            Assert.AreSame(Resolver.GetLazyFactory(typeof(IDisposable), new OptionsAttribute { Name = svcName }), Resolver.GetLazyFactory(typeof(IDisposable), new OptionsAttribute { Name = svcName }));
        }

        [Test]
        public void Get_ShouldCache()
        {
            Assert.AreSame(Resolver.Get(typeof(Disposable)), Resolver.Get(typeof(Disposable)));
            Assert.AreSame(Resolver.Get(typeof(Disposable).GetApplicableConstructor()), Resolver.Get(typeof(Disposable).GetApplicableConstructor()));
        }

        [Test]
        public void GetExtended_ShouldCache()
        {
            Assert.AreSame(Resolver.GetExtended(typeof(Disposable)), Resolver.GetExtended(typeof(Disposable)));
            Assert.AreSame(Resolver.GetExtended(typeof(Disposable).GetApplicableConstructor()), Resolver.GetExtended(typeof(Disposable).GetApplicableConstructor()));
        }

        public class MyInterceptor : InterfaceInterceptor<IList<IDictionary>> 
        {
            public MyInterceptor(IList<IDictionary> target, [Options(Optional = true)] IDisposable _) : base(target)
            {
            }
        }

        [Test]
        public void Get_ShouldSupportProxyTypes() 
        {
            Type proxy = ProxyGenerator<IList<IDictionary>, MyInterceptor>.GetGeneratedType();

            Func<IInjector, Type, object> factory = Resolver.Get(proxy);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<IDictionary>), null))
                .Returns(new List<IDictionary>());
            mockInjector
                .Setup(i => i.TryGet(typeof(IDisposable), null))
                .Returns(null);

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, typeof(IList<IDictionary>)));

            mockInjector.Verify(i => i.Get(typeof(IList<IDictionary>), null), Times.Once);
            mockInjector.Verify(i => i.TryGet(typeof(IDisposable), null), Times.Once);
        }

        [Test]
        public void GetExtended_ShouldSupportProxyTypes()
        {
            Type proxy = ProxyGenerator<IList<IDictionary>, MyInterceptor>.GetGeneratedType();

            Func<IInjector, IReadOnlyDictionary<string, object>, object> factory = Resolver.GetExtended(proxy);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<IDictionary>), null))
                .Returns(new List<IDictionary>());
            mockInjector
                .Setup(i => i.TryGet(typeof(IDisposable), null))
                .Returns(null);

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, new Dictionary<string, object>()));

            mockInjector.Verify(i => i.Get(typeof(IList<IDictionary>), null), Times.Once);
            mockInjector.Verify(i => i.TryGet(typeof(IDisposable), null), Times.Once);
        }
    }
}
