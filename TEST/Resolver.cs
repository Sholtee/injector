/********************************************************************************
* Resolver.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public sealed class ResolverTests
    {
        private class MyClass
        {
            public IDisposable Dep1 { get; }
            public ICloneable Dep2 { get; }

            public MyClass(IDisposable dep1, ICloneable dep2)
            {
                Dep1 = dep1;
                Dep2 = dep2;
            }

            public MyClass()
            {
            }
        }


        [Test]
        public void Resolver_ShouldResolveDependencies()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(It.IsAny<Type>()))
                .Returns<Type>(type =>
                {
                    if (type == typeof(IDisposable)) return new Disposable();
                    if (type == typeof(ICloneable))  return new InjectorEntry(null);

                    Assert.Fail("Unknown type");
                    return null;
                });

            Func<IInjector, Type, object> factory = Resolver
                .Create(typeof(MyClass)
                .GetConstructor(new[]
                {
                    typeof(IDisposable),
                    typeof(ICloneable)
                }));
            
            Assert.That(factory, Is.Not.Null);

            MyClass instance = (MyClass) factory(mockInjector.Object, null);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.InstanceOf<Disposable>());
            Assert.That(instance.Dep2, Is.InstanceOf<InjectorEntry>());
            
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(IDisposable))), Times.Once);
            mockInjector.Verify(i => i.Get(It.Is<Type>(t => t == typeof(ICloneable))),  Times.Once);
            mockInjector.Verify(i => i.Get(It.IsAny<Type>()), Times.Exactly(2));
        }

        [Test]
        public void Resolver_ShouldHandleParameterlessConstructors()
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector.Setup(i => i.Get(It.IsAny<Type>()));

            Func<IInjector, Type, object> factory = Resolver
                .Create(typeof(MyClass)
                .GetConstructor(new Type[0]));

            Assert.That(factory, Is.Not.Null);

            MyClass instance = (MyClass) factory(mockInjector.Object, null);

            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.Dep1, Is.Null);
            Assert.That(instance.Dep2, Is.Null);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>()), Times.Never);
        }
    }
}
