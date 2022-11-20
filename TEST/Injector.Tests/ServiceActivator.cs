/********************************************************************************
* ServiceActivator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public sealed class ServiceActivatorTests
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

        private class MyClass2
        {
            [Inject, Options(Name = "cica")]
            public IDisposable Dep1 { get; init; }
            [Inject]
            public IList Dep2 { get; init; }
            public int Int { get; init; }
        }

        private class MyClassHavingLazyProperty
        {
            [Inject, Options(Name = "cica")]
            public Lazy<IDisposable> Dep1 { get; init; }
            [Inject]
            public Lazy<IList> Dep2 { get; init; }
            public int Int { get; init; }
        }

        private class MyClassHavingOptionalProperty
        {
            [Inject, Options(Optional = true)]
            public IList Dep { get; init; }
        }

        public static IEnumerable<Func<ConstructorInfo, Func<IInjector, object>>> Activators
        {
            get
            {
                yield return ctor =>
                {
                    FactoryDelegate factory = ServiceActivator.Get(ctor).Compile();
                    return injector => factory(injector, null);
                };

                yield return ctor =>
                {
                    FactoryDelegate factory = ServiceActivator.Get(ctor, new Dictionary<string, object>(0)).Compile();
                    return injector => factory(injector, null);
                };

                yield return ctor =>
                {
                    FactoryDelegate factory = ServiceActivator.Get(ctor, new { }).Compile();
                    return injector => factory(injector, null);
                };
            }
        }

        [TestCase(0, typeof(IDisposable), null, typeof(IList), "cica")]
        [TestCase(0, typeof(Lazy<IDisposable>), "cica", typeof(Lazy<IList>), null)]
        [TestCase(1, typeof(IDisposable), null, typeof(IList), "cica")]
        [TestCase(1, typeof(Lazy<IDisposable>), "cica", typeof(Lazy<IList>), null)]
        [TestCase(2, typeof(IDisposable), null, typeof(IList), "cica")]
        [TestCase(2, typeof(Lazy<IDisposable>), "cica", typeof(Lazy<IList>), null)]
        public void Activator_ShouldResolveDependencies
        (
            int activatorFactoryIdx,
            Type dep1,
            string name1,
            Type dep2,
            string name2
        )
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            var mockDisposable = new Mock<IDisposable>();
            var mockList = new Mock<IList>();

            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) =>
                {
                    if (type == typeof(IDisposable) && name == name1)
                        return mockDisposable.Object;
                    if (type == typeof(IList) && name == name2)
                        return mockList.Object;

                    Assert.Fail("Unknown type");
                    return null;
                });
                   
            MyClass instance = (MyClass) Activators.ToList()[activatorFactoryIdx]
            (
                typeof(MyClass).GetConstructor(new[] {dep1, dep2})
            ).Invoke(mockInjector.Object);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.SameAs(mockDisposable.Object));
            Assert.That(instance.Dep2, Is.SameAs(mockList.Object));
            
            mockInjector.Verify(i => i.Get(typeof(IDisposable), name1), Times.Once);
            mockInjector.Verify(i => i.Get(typeof(IList), name2), Times.Once);
            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void Activator_ShouldResolveProperties([ValueSource(nameof(Activators))] Func<ConstructorInfo, Func<IInjector, object>> activatorFactory)
        {
            var mockDisposable = new Mock<IDisposable>();
            var mockList = new Mock<IList>();
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) =>
                {
                    if (type == typeof(IDisposable) && name == "cica") return mockDisposable.Object;
                    if (type == typeof(IList)) return mockList.Object;

                    Assert.Fail("Unknown type");
                    return null;
                });

            MyClass2 instance = (MyClass2) activatorFactory(typeof(MyClass2).GetConstructor(Type.EmptyTypes)).Invoke(mockInjector.Object);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.SameAs(mockDisposable.Object));
            Assert.That(instance.Dep2, Is.SameAs(mockList.Object));

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), It.Is<string>(n => n == "cica")), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IList)), It.Is<string>(n => n == null)), Times.Once);
            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void Activator_ShouldResolveLazyProperties([ValueSource(nameof(Activators))] Func<ConstructorInfo, Func<IInjector, object>> activatorFactory)
        {
            var mockDisposable = new Mock<IDisposable>();
            var mockList = new Mock<IList>();
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) =>
                {
                    if (type == typeof(IDisposable) && name == "cica") return mockDisposable.Object;
                    if (type == typeof(IList)) return mockList.Object;

                    Assert.Fail("Unknown type");
                    return null;
                });

            MyClassHavingLazyProperty instance = (MyClassHavingLazyProperty) activatorFactory(typeof(MyClassHavingLazyProperty).GetConstructor(Type.EmptyTypes)).Invoke(mockInjector.Object);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.Not.Null);
            Assert.That(instance.Dep2, Is.Not.Null);
            Assert.That(instance.Dep1.Value, Is.SameAs(mockDisposable.Object));
            Assert.That(instance.Dep2.Value, Is.SameAs(mockList.Object));

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), It.Is<string>(n => n == "cica")), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IList)), It.Is<string>(n => n == null)), Times.Once);
            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void Activator_ShouldResolveOptionalProperties()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.TryGet(typeof(IList), null))
                .Returns(new List<IDictionary>());

            FactoryDelegate factory = ServiceActivator.Get(typeof(MyClassHavingOptionalProperty)).Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, typeof(IList)));

            mockInjector.Verify(i => i.TryGet(typeof(IList), null), Times.Once);
        }

        [Test]
        public void Get_ShouldHandleParameterlessConstructors()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()));

            FactoryDelegate factory = ServiceActivator.Get(typeof(MyClass).GetConstructor(Type.EmptyTypes)).Compile();

            Assert.That(factory, Is.Not.Null);

            MyClass instance = (MyClass) factory(mockInjector.Object, null);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.Null);
            Assert.That(instance.Dep2, Is.Null);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExtendedActivator_ShouldSupportExplicitArguments()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()));

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(List<string>).GetConstructor(new[] {typeof(int)}), new Dictionary<string, object>
                {
                    {"capacity", 10}
                })
                .Compile();

            object lst = factory(mockInjector.Object, null);

            Assert.That(lst, Is.InstanceOf<List<string>>());
            Assert.That(((List<string>) lst).Capacity, Is.EqualTo(10));
           
            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExtendedActivator_ShouldSupportExplicitArguments2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()));

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(List<string>).GetConstructor(new[] { typeof(int) }), new { capacity = 10 })
                .Compile();

            object lst = factory(mockInjector.Object, null);

            Assert.That(lst, Is.InstanceOf<List<string>>());
            Assert.That(((List<string>)lst).Capacity, Is.EqualTo(10));

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExtendedActivator_ShouldSupportExplicitArguments3()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()));

            ApplyProxyDelegate factory = ServiceActivator
                .GetLateBound(typeof(List<string>).GetConstructor(new[] { typeof(int) }), 0)
                .Compile();

            object lst = factory(mockInjector.Object, null, 10);

            Assert.That(lst, Is.InstanceOf<List<string>>());
            Assert.That(((List<string>)lst).Capacity, Is.EqualTo(10));

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExtendedActivator_ShouldSupportExplicitProperties()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(MyClassHavingOptionalProperty).GetConstructor(Type.EmptyTypes), new Dictionary<string, object>
                {
                    {nameof(MyClassHavingOptionalProperty.Dep), new List<string>()}
                })
                .Compile();

            var ret = (MyClassHavingOptionalProperty) factory(mockInjector.Object, null);

            Assert.That(ret, Is.InstanceOf<MyClassHavingOptionalProperty>());
            Assert.That(ret.Dep, Is.Not.Null);
        }

        [Test]
        public void ExtendedActivator_ShouldSupportExplicitProperties2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(MyClassHavingOptionalProperty).GetConstructor(Type.EmptyTypes), new
                {
                    Dep = (IList)new List<string>()
                })
                .Compile();

            var ret = (MyClassHavingOptionalProperty)factory(mockInjector.Object, null);

            Assert.That(ret, Is.InstanceOf<MyClassHavingOptionalProperty>());
            Assert.That(ret.Dep, Is.Not.Null);
        }

        [Test]
        public void ExtendedActivator_ShouldResolveOptionalArguments() 
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<IDictionary>), null))
                .Returns(new List<IDictionary>());
            mockInjector
                .Setup(i => i.TryGet(typeof(IDisposable), null))
                .Returns(null);

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(MyInterceptor), new Dictionary<string, object>())
                .Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));

            mockInjector.Verify(i => i.Get(typeof(IList<IDictionary>), null), Times.Once);
            mockInjector.Verify(i => i.TryGet(typeof(IDisposable), null), Times.Once);
        }

        [Test]
        public void ExtendedActivator_ShouldResolveOptionalArguments2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<IDictionary>), null))
                .Returns(new List<IDictionary>());
            mockInjector
                .Setup(i => i.TryGet(typeof(IDisposable), null))
                .Returns(null);

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(MyInterceptor), new { })
                .Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));

            mockInjector.Verify(i => i.Get(typeof(IList<IDictionary>), null), Times.Once);
            mockInjector.Verify(i => i.TryGet(typeof(IDisposable), null), Times.Once);
        }

        [Test]
        public void ExtendedActivator_ShouldResolveOptionalProperties()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.TryGet(typeof(IList), null))
                .Returns(new List<IDictionary>());

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(MyClassHavingOptionalProperty), new Dictionary<string, object>())
                .Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));

            mockInjector.Verify(i => i.TryGet(typeof(IList), null), Times.Once);
        }

        [Test]
        public void ExtendedActivator_ShouldResolveOptionalProperties2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.TryGet(typeof(IList), null))
                .Returns(new List<IDictionary>());

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(MyClassHavingOptionalProperty), new {})
                .Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));

            mockInjector.Verify(i => i.TryGet(typeof(IList), null), Times.Once);
        }

        [Test]
        public void ExtendedActivator_ShouldResolveLazyArguments()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Loose);

            FactoryDelegate factory =  ServiceActivator
                .Get(typeof(MyClass).GetConstructor(new[] { typeof(Lazy<IDisposable>), typeof(Lazy<IList>) }), new Dictionary<string, object>())
                .Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));
        }

        [Test]
        public void ExtendedActivator_ShouldResolveLazyArguments2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Loose);

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(MyClass).GetConstructor(new[] { typeof(Lazy<IDisposable>), typeof(Lazy<IList>) }), new { })
                .Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));
        }

        [Test]
        public void ExtendedActivator_ShouldThrowOnNonInterfaceArguments()
        {
            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList), typeof(int) });

            //
            // Sima Get() hivas nem fog mukodni (nem interface parameter).
            //

            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(ctor), Resources.INVALID_CONSTRUCTOR);

            //
            // Ne mock-oljuk az injector-t h a megfelelo kiveteleket kapjuk
            //

            using (IScopeFactory root = ScopeFactory.Create(svcs => svcs
                .Factory<IDisposable>(i => new Disposable(false), Lifetime.Scoped)
                .Factory<IList>(i => new List<object>(), Lifetime.Scoped)))
            {
                IInjector injector = root.CreateScope();

                //
                // Ez mukodne viszont nem adtuk meg a nem interface parametert.
                //

                FactoryDelegate factory = ServiceActivator
                    .Get(ctor, new Dictionary<string, object>(0))
                    .Compile();

                Assert.Throws<ArgumentException>(() => factory(injector, null), Resources.PARAMETER_NOT_AN_INTERFACE);
            }
        }

        [Test]
        public void ExtendedActivator_ShouldThrowOnNonInterfaceArguments2()
        {
            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList), typeof(int) });

            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(ctor, new { }), Resources.INVALID_CONSTRUCTOR);
        }

        [Test]
        public void Get_ShouldValidate() 
        {
            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(typeof(IDisposable)), Resources.PARAMETER_NOT_A_CLASS);
            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(typeof(IList<>)), Resources.PARAMETER_IS_GENERIC);
            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(typeof(AbstractClass)), Resources.PARAMETER_IS_ABSTRACT);
        }

        [Test]
        public void GetExtended_ShouldValidate()
        {
            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(typeof(IDisposable), new { }), Resources.PARAMETER_NOT_A_CLASS);
            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(typeof(IList<>), new { }), Resources.PARAMETER_IS_GENERIC);
            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(typeof(AbstractClass), new { }), Resources.PARAMETER_IS_ABSTRACT);
        }

        [Test]
        public void GetExtended_ShouldValidate2()
        {
            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(typeof(IDisposable), new Dictionary<string, object>()), Resources.PARAMETER_NOT_A_CLASS);
            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(typeof(IList<>), new Dictionary<string, object>()), Resources.PARAMETER_IS_GENERIC);
            Assert.Throws<ArgumentException>(() => ServiceActivator.Get(typeof(AbstractClass), new Dictionary<string, object>()), Resources.PARAMETER_IS_ABSTRACT);
        }

        private abstract class AbstractClass { }

        [Test]
        public void ExtendedActivator_ExplicitArgumentsShouldSuppressTheInjectorInvocation()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => null);

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(MyClass).GetConstructor(new[] {typeof(IDisposable), typeof(IList) }), new Dictionary<string, object>
                {
                    {"dep2", null}
                })
                .Compile();

            object obj = factory(mockInjector.Object, null);

            Assert.That(obj, Is.InstanceOf<MyClass>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IList)), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExtendedActivator_ExplicitArgumentsShouldSuppressTheInjectorInvocation2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => null);

            FactoryDelegate factory = ServiceActivator
                .Get(typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList) }), new
                {
                    dep2 = (IList) null
                })
                .Compile();

            object obj = factory(mockInjector.Object, null);

            Assert.That(obj, Is.InstanceOf<MyClass>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IList)), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExtendedActivator_ExplicitArgumentsShouldSuppressTheInjectorInvocation3()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => null);

            ApplyProxyDelegate factory = ServiceActivator
                .GetLateBound(typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList) }), 1)
                .Compile();

            object obj = factory(mockInjector.Object, null, null);

            Assert.That(obj, Is.InstanceOf<MyClass>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IList)), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ExtendedActivator_ExplicitArgumentsMayContainNonInterfaceValues()
        {
            const int TEN = 10;

            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList), typeof(int) });

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => null);

            MyClass obj = ServiceActivator
                .Get(ctor, new Dictionary<string, object> { { "int", TEN } })
                .Compile()
                .Invoke(mockInjector.Object, null) as MyClass;

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.Int, Is.EqualTo(TEN));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void CreateLazy_ShouldReturnProperLazyInstance(string svcName)
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => new Disposable());

            ParameterExpression injector = Expression.Parameter(typeof(IInjector));

            Func<IInjector, Lazy<IDisposable>> factory = Expression.Lambda<Func<IInjector, Lazy<IDisposable>>>
            (
                ServiceActivator.CreateLazy(injector, typeof(IDisposable), new OptionsAttribute { Name = svcName }),
                injector
            ).Compile();

            Lazy<IDisposable> lazy = factory(mockInjector.Object);
            Assert.That(lazy, Is.Not.Null);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);

            IDisposable retval = lazy.Value;
            Assert.That(retval, Is.InstanceOf<Disposable>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), It.Is<string>(s => s == svcName)), Times.Once);
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

            FactoryDelegate factory = ServiceActivator.Get(proxy).Compile();

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
        public void Get_ShouldSupportProxyTypes2()
        {
            Type proxy = ProxyGenerator<IList<IDictionary>, MyInterceptor>.GetGeneratedType();

            FactoryDelegate factory = ServiceActivator.Get(proxy, new Dictionary<string, object>()).Compile();

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<IDictionary>), null))
                .Returns(new List<IDictionary>());
            mockInjector
                .Setup(i => i.TryGet(typeof(IDisposable), null))
                .Returns(null);

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));

            mockInjector.Verify(i => i.Get(typeof(IList<IDictionary>), null), Times.Once);
            mockInjector.Verify(i => i.TryGet(typeof(IDisposable), null), Times.Once);
        }

        [Test]
        public void Get_ShouldSupportProxyTypes3()
        {
            Type proxy = ProxyGenerator<IList<IDictionary>, MyInterceptor>.GetGeneratedType();

            FactoryDelegate factory = ServiceActivator.Get(proxy, new { }).Compile();

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<IDictionary>), null))
                .Returns(new List<IDictionary>());
            mockInjector
                .Setup(i => i.TryGet(typeof(IDisposable), null))
                .Returns(null);

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));

            mockInjector.Verify(i => i.Get(typeof(IList<IDictionary>), null), Times.Once);
            mockInjector.Verify(i => i.TryGet(typeof(IDisposable), null), Times.Once);
        }

        [Test]
        public void Get_ShouldSupportProxyTypes4()
        {
            Type proxy = ProxyGenerator<IList<IDictionary>, MyInterceptor>.GetGeneratedType();

            ApplyProxyDelegate factory = ServiceActivator.GetLateBound(proxy.GetConstructor(new[] { typeof(IList<IDictionary>), typeof(IDisposable) }), 100).Compile();

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<IDictionary>), null))
                .Returns(new List<IDictionary>());
            mockInjector
                .Setup(i => i.TryGet(typeof(IDisposable), null))
                .Returns(null);

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null, null));

            mockInjector.Verify(i => i.Get(typeof(IList<IDictionary>), null), Times.Once);
            mockInjector.Verify(i => i.TryGet(typeof(IDisposable), null), Times.Once);
        }
    }
}
