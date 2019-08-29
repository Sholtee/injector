/********************************************************************************
* Resolver.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Properties;

    [TestFixture]
    public sealed class ResolverTests
    {
        private class MyClass
        {
            public IDisposable Dep1 { get; }
            public IServiceFactory Dep2 { get; }
            public int Int { get; }

            public MyClass(IDisposable dep1, IServiceFactory dep2)
            {
                Dep1 = dep1;
                Dep2 = dep2;
            }

            public MyClass(IDisposable dep1, IServiceFactory dep2, int @int)
            {
                Dep1 = dep1;
                Dep2 = dep2;
                Int  = @int;
            }

            public MyClass(Lazy<IDisposable> dep1, Lazy<IServiceFactory> dep2)
            {
                Dep1 = dep1.Value;
                Dep2 = dep2.Value;
            }

            public MyClass()
            {
            }
        }

        [TestCase(typeof(IDisposable), typeof(IServiceFactory))]
        [TestCase(typeof(Lazy<IDisposable>), typeof(Lazy<IServiceFactory>))]
        public void Resolver_ShouldResolveDependencies(Type dep1, Type dep2)
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns<Type, Type>((type, target) =>
                {
                    if (type == typeof(IDisposable)) return new Disposable();
                    if (type == typeof(IServiceFactory)) return new TransientServiceEntry(type, factory: null, owner: null);

                    Assert.Fail("Unknown type");
                    return null;
                });

            Func<IInjector, object> factory = Resolver.Get(typeof(MyClass).GetConstructor(new[] {dep1, dep2}));
            
            Assert.That(factory, Is.Not.Null);

            MyClass instance = (MyClass) factory(mockInjector.Object);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.InstanceOf<Disposable>());
            Assert.That(instance.Dep2, Is.InstanceOf<AbstractServiceEntry>());
            
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), It.Is<Type>(t => t == typeof(MyClass))), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IServiceFactory)),  It.Is<Type>(t => t == typeof(MyClass))), Times.Once);
            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<Type>()), Times.Exactly(2));
        }

        [Test]
        public void Resolver_ShouldHandleParameterlessConstructors()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<Type>()));

            Func<IInjector, object> factory = Resolver
                .Get(typeof(MyClass).GetConstructor(new Type[0]));

            Assert.That(factory, Is.Not.Null);

            MyClass instance = (MyClass) factory(mockInjector.Object);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.Null);
            Assert.That(instance.Dep2, Is.Null);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<Type>()), Times.Never);
        }

        [Test]
        public void Resolver_ShouldHandleExplicitArguments()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<Type>()));

            Func<IInjector, IReadOnlyDictionary<string, object>, object> factory = Resolver
                .GetExtended(typeof(List<string>).GetConstructor(new[] {typeof(int)}));

            object lst = factory(mockInjector.Object, new Dictionary<string, object>
            {
                {"capacity", 10}
            });

            Assert.That(lst, Is.InstanceOf<List<string>>());
            Assert.That(((List<string>) lst).Capacity, Is.EqualTo(10));
           
            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), It.IsAny<Type>()), Times.Never);
        }

        [Test]
        public void Resolver_ShouldThrowOnNonInterfaceArguments()
        {
            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IServiceFactory), typeof(int) });

            Assert.Throws<ArgumentException>(() => Resolver.Get(ctor), Resources.INVALID_CONSTRUCTOR);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns<Type, Type>((type, target) => null);

            Assert.Throws<ArgumentException>(() => Resolver.GetExtended(ctor)(mockInjector.Object, new Dictionary<string, object>(0)), Resources.INVALID_CONSTRUCTOR_ARGUMENT);
        }

        [Test]
        public void Resolver_ExplicitArgumentsShouldSuppressTheInjectorInvocation()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns<Type, Type>((type, target) => null);

            Func<IInjector, IReadOnlyDictionary<string, object>, object> factory = Resolver
                .GetExtended(typeof(MyClass).GetConstructor(new[] {typeof(IDisposable), typeof(IServiceFactory) }));

            object obj = factory(mockInjector.Object, new Dictionary<string, object>
            {
                {"dep2", null}
            });

            Assert.That(obj, Is.InstanceOf<MyClass>());

            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable)), It.Is<Type>(t => t == typeof(MyClass))), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IServiceFactory)),  It.Is<Type>(t => t == typeof(MyClass))), Times.Never);
        }

        [Test]
        public void Resolver_ExplicitArgumentsCanBeNonInterfaceValues()
        {
            const int TEN = 10;

            ConstructorInfo ctor = typeof(MyClass).GetConstructor(new[] { typeof(IDisposable), typeof(IServiceFactory), typeof(int) });

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns<Type, Type>((type, target) => null);

            MyClass obj = Resolver.GetExtended(ctor)(mockInjector.Object, new Dictionary<string, object> { { "int", TEN } }) as MyClass;

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.Int, Is.EqualTo(TEN));
        }

        [Test]
        public void Resolver_GetLazyFactory_ShouldReturnProperFactory()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>(), It.IsAny<Type>()))
                .Returns<Type, Type>((type, target) => new Disposable());

            Func<IInjector, Type, object> factory = Resolver.GetLazyFactory(typeof(IDisposable));
            Assert.That(factory, Is.Not.Null);

            Lazy<IDisposable> lazy = factory(mockInjector.Object, null) as Lazy<IDisposable>;
            Assert.That(lazy, Is.Not.Null);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), null), Times.Never);

            IDisposable retval = lazy.Value;
            Assert.That(retval, Is.InstanceOf<Disposable>());

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), null), Times.Once);
        }

        [Test]
        public void Resolver_GetLazyFactory_ShouldCache()
        {
            Assert.AreSame(Resolver.GetLazyFactory(typeof(IDisposable)), Resolver.GetLazyFactory(typeof(IDisposable)));
        }
    }
}
