﻿/********************************************************************************
* ServiceActivator.Factory.cs                                                   *
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

    [TestFixture]
    public sealed partial class ServiceActivatorTests
    {
        private static Expression<FactoryDelegate>  ResolveFactory(ConstructorInfo constructor, object userData, ServiceOptions opts) =>
            new ServiceActivator(opts).ResolveFactory(constructor, userData);

        private class MyClass
        {
            public IDisposable Dep1 { get; }
            public IList Dep2 { get; }
            public int Int { get; }

            [ServiceActivator]
            public MyClass(IDisposable dep1, [Options(Key = "cica")] IList dep2)
            {
                Dep1 = dep1;
                Dep2 = dep2;
            }

            public MyClass(IDisposable dep1, IList dep2, int _int)
            {
                Dep1 = dep1;
                Dep2 = dep2;
                Int  = _int;
            }

            public MyClass([Options(Key = "cica")] Lazy<IDisposable> dep1, Lazy<IList> dep2)
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
            [Inject, Options(Key = "cica")]
            public IDisposable Dep1 { get; init; }
            [Inject]
            public IList Dep2 { get; init; }
            public int Int { get; init; }
        }

        private sealed class MyClassHavingInvalidInject
        {
            [Inject]
            public IList Dep { get; }
        }

        private class MyClassHavingLazyProperty
        {
            [Inject, Options(Key = "cica")]
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

        public static IEnumerable<Func<ConstructorInfo, ServiceOptions, Func<IInjector, object>>> Activators
        {
            get
            {
                yield return (ctor, opts) =>
                {
                    FactoryDelegate factory = ResolveFactory(ctor, null, opts).Compile();
                    return injector => factory(injector, null);
                };

                yield return (ctor, opts) =>
                {
                    FactoryDelegate factory = ResolveFactory(ctor, new Dictionary<string, object>(0), opts).Compile();
                    return injector => factory(injector, null);
                };

                yield return (ctor, opts) =>
                {
                    FactoryDelegate factory = ResolveFactory(ctor, new { }, opts).Compile();
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
        public void Factory_ShouldResolveDependencies
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
                typeof(MyClass).GetConstructor(new[] {dep1, dep2}),
                null
            ).Invoke(mockInjector.Object);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.SameAs(mockDisposable.Object));
            Assert.That(instance.Dep2, Is.SameAs(mockList.Object));
            
            mockInjector.Verify(i => i.Get(typeof(IDisposable), name1), Times.Once);
            mockInjector.Verify(i => i.Get(typeof(IList), name2), Times.Once);
            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void Factory_ShouldResolveProperties([ValueSource(nameof(Activators))] Func<ConstructorInfo, ServiceOptions, Func<IInjector, object>> activatorFactory)
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

            MyClass2 instance = (MyClass2) activatorFactory(typeof(MyClass2).GetConstructor(Type.EmptyTypes), null).Invoke(mockInjector.Object);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.SameAs(mockDisposable.Object));
            Assert.That(instance.Dep2, Is.SameAs(mockList.Object));

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), It.Is<string>(n => n == "cica")), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IList)), It.Is<string>(n => n == null)), Times.Once);
            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Test]
        public void Factory_ShouldResolveCompatiblePropertiesOnly([ValueSource(nameof(Activators))] Func<ConstructorInfo, ServiceOptions, Func<IInjector, object>> activatorFactory)
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            MyClassHavingInvalidInject obj = (MyClassHavingInvalidInject) activatorFactory(typeof(MyClassHavingInvalidInject).GetApplicableConstructor(), null).Invoke(mockInjector.Object);

            Assert.IsNull(obj.Dep);
        }
#if NET7_0_OR_GREATER
        private sealed class MyClassHavingRequiredProperty
        {
            public required IList Dep { get; set; }
        }

        [Test]
        public void Factory_ShouldResolveRequiredProperties([ValueSource(nameof(Activators))] Func<ConstructorInfo, ServiceOptions, Func<IInjector, object>> activatorFactory, [Values(true, false)] bool autoInject)
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList), null))
                .Returns(Array.Empty<object>());

            MyClassHavingRequiredProperty obj = (MyClassHavingRequiredProperty) activatorFactory(typeof(MyClassHavingRequiredProperty).GetApplicableConstructor(), ServiceOptions.Default with { AutoInject = autoInject }).Invoke(mockInjector.Object);

            Assert.That(obj.Dep is not null, Is.EqualTo(autoInject));
        }
#endif
        [Test]
        public void Factory_ShouldResolveLazyProperties([ValueSource(nameof(Activators))] Func<ConstructorInfo, ServiceOptions, Func<IInjector, object>> activatorFactory)
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

            MyClassHavingLazyProperty instance = (MyClassHavingLazyProperty) activatorFactory(typeof(MyClassHavingLazyProperty).GetConstructor(Type.EmptyTypes), null).Invoke(mockInjector.Object);

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
        public void Factory_ShouldResolveOptionalProperties()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.TryGet(typeof(IList), null))
                .Returns(new List<IDictionary>());

            FactoryDelegate factory = ResolveFactory(typeof(MyClassHavingOptionalProperty).GetApplicableConstructor(), null, null).Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, typeof(IList)));

            mockInjector.Verify(i => i.TryGet(typeof(IList), null), Times.Once);
        }

        [Test]
        public void Factory_ShouldHandleParameterlessConstructors()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()));

            FactoryDelegate factory = ResolveFactory(typeof(MyClass).GetConstructor(Type.EmptyTypes), null, null).Compile();

            Assert.That(factory, Is.Not.Null);

            MyClass instance = (MyClass) factory(mockInjector.Object, null);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.Null);
            Assert.That(instance.Dep2, Is.Null);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Factory_ShouldSupportExplicitArguments()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()));

            FactoryDelegate factory = ResolveFactory
            (
                typeof(List<string>).GetConstructor(new[] {typeof(int)}),
                new Dictionary<string, object>
                {
                    {"capacity", 10}
                },
                null
            ).Compile();

            object lst = factory(mockInjector.Object, null);

            Assert.That(lst, Is.InstanceOf<List<string>>());
            Assert.That(((List<string>) lst).Capacity, Is.EqualTo(10));
           
            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Factory_ShouldSupportExplicitArguments2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()));

            FactoryDelegate factory = ResolveFactory(typeof(List<string>).GetConstructor(new[] { typeof(int) }), new { capacity = 10 }, null)
                .Compile();

            object lst = factory(mockInjector.Object, null);

            Assert.That(lst, Is.InstanceOf<List<string>>());
            Assert.That(((List<string>)lst).Capacity, Is.EqualTo(10));

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Factory_ShouldSupportExplicitProperties()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            FactoryDelegate factory = ResolveFactory
            (
                typeof(MyClassHavingOptionalProperty).GetConstructor(Type.EmptyTypes),
                new Dictionary<string, object>
                {
                    {nameof(MyClassHavingOptionalProperty.Dep), new List<string>()}
                },
                null
            ).Compile();

            var ret = (MyClassHavingOptionalProperty) factory(mockInjector.Object, null);

            Assert.That(ret, Is.InstanceOf<MyClassHavingOptionalProperty>());
            Assert.That(ret.Dep, Is.Not.Null);
        }

        [Test]
        public void Factory_ShouldSupportExplicitProperties2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            FactoryDelegate factory = ResolveFactory
            (
                typeof(MyClassHavingOptionalProperty).GetConstructor(Type.EmptyTypes),
                new
                {
                    Dep = (IList) new List<string>()
                },
                null
            ).Compile();

            var ret = (MyClassHavingOptionalProperty)factory(mockInjector.Object, null);

            Assert.That(ret, Is.InstanceOf<MyClassHavingOptionalProperty>());
            Assert.That(ret.Dep, Is.Not.Null);
        }

        private sealed class MyServiceHavingDependency
        {
            public MyServiceHavingDependency(IList<IDictionary> _, [Options(Optional = true)] IDisposable __)
            {
            }
        }

        [Test]
        public void Factory_UsingExplicitArgs_ShouldResolveOptionalArguments() 
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<IDictionary>), null))
                .Returns(new List<IDictionary>());
            mockInjector
                .Setup(i => i.TryGet(typeof(IDisposable), null))
                .Returns(null);

            FactoryDelegate factory = ResolveFactory(typeof(MyServiceHavingDependency).GetApplicableConstructor(), new Dictionary<string, object>(), null)
                .Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));

            mockInjector.Verify(i => i.Get(typeof(IList<IDictionary>), null), Times.Once);
            mockInjector.Verify(i => i.TryGet(typeof(IDisposable), null), Times.Once);
        }

        [Test]
        public void Factory_UsingExplicitArgs_ShouldResolveOptionalArguments2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList<IDictionary>), null))
                .Returns(new List<IDictionary>());
            mockInjector
                .Setup(i => i.TryGet(typeof(IDisposable), null))
                .Returns(null);

            FactoryDelegate factory = ResolveFactory(typeof(MyServiceHavingDependency).GetApplicableConstructor(), new { }, null)
                .Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));

            mockInjector.Verify(i => i.Get(typeof(IList<IDictionary>), null), Times.Once);
            mockInjector.Verify(i => i.TryGet(typeof(IDisposable), null), Times.Once);
        }

        [Test]
        public void Factory_UsingExplicitArgs_ShouldResolveOptionalProperties()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.TryGet(typeof(IList), null))
                .Returns(new List<IDictionary>());

            FactoryDelegate factory = ResolveFactory(typeof(MyClassHavingOptionalProperty).GetApplicableConstructor(), new Dictionary<string, object>(), null)
                .Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));

            mockInjector.Verify(i => i.TryGet(typeof(IList), null), Times.Once);
        }

        [Test]
        public void Factory_UsingExplicitArgs_ShouldResolveOptionalProperties2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.TryGet(typeof(IList), null))
                .Returns(new List<IDictionary>());

            FactoryDelegate factory = ResolveFactory(typeof(MyClassHavingOptionalProperty).GetApplicableConstructor(), new {}, null)
                .Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));

            mockInjector.Verify(i => i.TryGet(typeof(IList), null), Times.Once);
        }

        [Test]
        public void Factory_UsingExplicitArgs_ShouldResolveLazyArguments()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Loose);

            FactoryDelegate factory = ResolveFactory
            (
                typeof(MyClass).GetConstructor(new[] { typeof(Lazy<IDisposable>), typeof(Lazy<IList>) }),
                new Dictionary<string, object>(),
                null
            ).Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));
        }

        [Test]
        public void Factory_UsingExplicitArgs_ShouldResolveLazyArguments2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Loose);

            FactoryDelegate factory = ResolveFactory
            (
                typeof(MyClass).GetConstructor(new[] { typeof(Lazy<IDisposable>), typeof(Lazy<IList>) }),
                new { },
                null
            ).Compile();

            Assert.DoesNotThrow(() => factory.Invoke(mockInjector.Object, null));
        }

        [Test]
        public void Resolve_ShouldThrowOnNonInterfaceArguments()
        {
            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList), typeof(int) });

            Assert.Throws<ArgumentException>(() => ResolveFactory(ctor, null, null), Resources.INVALID_DEPENDENCY);
        }

        [Test]
        public void Resolve_ShouldThrowOnNonInterfaceArguments2()
        {
            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList), typeof(int) });

            Assert.Throws<ArgumentException>(() => ResolveFactory(ctor, new { }, null), Resources.INVALID_DEPENDENCY);
        }

        [Test]
        public void DependencyResolvers_MayBeAltered()
        {
            Mock<IReadOnlyList<IDependencyResolver>> mockResolvers = new(MockBehavior.Strict);
            mockResolvers
                .SetupGet(res => res.Count)
                .Returns(1);
            mockResolvers
                .SetupGet(res => res[0])
                .Returns(DefaultDependencyResolvers.Value[DefaultDependencyResolvers.Value.Count - 1]);

            new SingletonServiceEntry(typeof(MyClass), null, typeof(MyClass), ServiceOptions.Default with { DependencyResolvers = mockResolvers.Object });

            mockResolvers.VerifyGet(res => res[0], Times.AtLeastOnce);
        }

        private abstract class AbstractClass { }

        [Test]
        public void Factory_ExplicitArgumentsShouldSuppressTheInjectorInvocation()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => null);

            FactoryDelegate factory = ResolveFactory
            (
                typeof(MyClass).GetConstructor(new[] {typeof(IDisposable), typeof(IList) }),
                new Dictionary<string, object>
                {
                    {"dep2", null}
                },
                null
            ).Compile();

            object obj = factory(mockInjector.Object, null);

            Assert.That(obj, Is.InstanceOf<MyClass>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IList)), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Factory_ExplicitArgumentsShouldSuppressTheInjectorInvocation2()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => null);

            FactoryDelegate factory = ResolveFactory
            (
                typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList) }),
                new
                {
                    dep2 = (IList) null
                },
                null
            ).Compile();

            object obj = factory(mockInjector.Object, null);

            Assert.That(obj, Is.InstanceOf<MyClass>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), null), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IList)), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void Factory_ExplicitArgumentsMayContainNonInterfaceValues()
        {
            const int TEN = 10;

            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList), typeof(int) });

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => null);

            MyClass obj = ResolveFactory(ctor, new Dictionary<string, object> { { "_int", TEN } }, null)
                .Compile()
                .Invoke(mockInjector.Object, null) as MyClass;

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.Int, Is.EqualTo(TEN));
        }

        [Test]
        public void Factory_ExplicitArgumentsMayContainNonInterfaceValues2()
        {
            const int TEN = 10;

            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IList), typeof(int) });

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => null);

            MyClass obj = ResolveFactory(ctor, new { _int = TEN }, null)
                .Compile()
                .Invoke(mockInjector.Object, null) as MyClass;

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.Int, Is.EqualTo(TEN));
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void ResolveRegularLazyService_ShouldReturnProperLazyInstance(string svcName)
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => new Disposable());

            ParameterExpression injector = Expression.Parameter(typeof(IInjector));

            Func<IInjector, Lazy<IDisposable>> factory = Expression.Lambda<Func<IInjector, Lazy<IDisposable>>>
            (
                new RegularLazyDependencyResolver().ResolveLazyService(injector, typeof(IDisposable), new OptionsAttribute { Key = svcName }),
                injector
            ).Compile();

            Lazy<IDisposable> lazy = factory(mockInjector.Object);
            Assert.That(lazy, Is.Not.Null);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);

            IDisposable retval = lazy.Value;
            Assert.That(retval, Is.InstanceOf<Disposable>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), It.Is<string>(s => s == svcName)), Times.Once);
        }

        [TestCase(null)]
        [TestCase("cica")]
        public void ResolveLazyService_ShouldReturnProperLazyInstance(string svcName)
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns<Type, string>((type, name) => new Disposable());

            ParameterExpression injector = Expression.Parameter(typeof(IInjector));

            Func<IInjector, ILazy<IDisposable>> factory = Expression.Lambda<Func<IInjector, ILazy<IDisposable>>>
            (
                new LazyDependencyResolver().ResolveLazyService(injector, typeof(IDisposable), new OptionsAttribute { Key = svcName }),
                injector
            ).Compile();

            ILazy<IDisposable> lazy = factory(mockInjector.Object);
            Assert.That(lazy, Is.Not.Null);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<string>()), Times.Never);

            IDisposable retval = lazy.Value;
            Assert.That(retval, Is.InstanceOf<Disposable>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), It.Is<string>(s => s == svcName)), Times.Once);
        }
    }
}
