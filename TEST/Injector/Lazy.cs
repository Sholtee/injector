/********************************************************************************
* Lazy.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        [Test]
        public void Injector_Lazy_ShouldBeAService()
        {
            var mockTypeResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockTypeResolver.Setup(i => i.Resolve(It.IsAny<Type>()));

            Injector.Lazy<IInterface_1>(mockTypeResolver.Object);

            IServiceInfo serviceInfo = Injector.QueryServiceInfo<IInterface_1>();

            Assert.That(serviceInfo.IsService);
            Assert.That(serviceInfo.IsLazy);
            Assert.False(serviceInfo.IsFactory);
            Assert.False(serviceInfo.IsInstance);

            mockTypeResolver.Verify(i => i.Resolve(It.IsAny<Type>()), Times.Never);
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_Lazy_ShouldCallTheResolverOnRequest(Lifetime lifetime)
        {
            var mockResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(typeof(Implementation_1));

            Injector.Lazy<IInterface_1>(mockResolver.Object, lifetime);

            //
            // Az elso Get()-eleskor kell hivja a rendszer a resolver-t
            //

            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Never);
            var instance = Injector.Get<IInterface_1>();
            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Once);

            Assert.That(instance, Is.InstanceOf<Implementation_1>());
            (lifetime == Lifetime.Singleton ? Assert.AreSame : (Action<object, object>) Assert.AreNotSame)(instance, Injector.Get<IInterface_1>());
            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Once);
        }

        [Test]
        public void Injector_Lazy_ShouldBeTypeChecked()
        {
            var mockResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(typeof(Implementation_2));

            Injector.Lazy<IInterface_1>(mockResolver.Object);

            Assert.Throws<InvalidOperationException>(() => Injector.Get<IInterface_1>(), string.Format(Resources.NOT_ASSIGNABLE, typeof(IInterface_1), typeof(object)));
        }

        [Test]
        public void Injector_Lazy_ShouldHandleOpenGenericTypes()
        {
            var mockResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<>))))
                .Returns(typeof(Implementation_3<>));

            Injector
                .Service<IInterface_1, Implementation_1>()
                .Lazy(typeof(IInterface_3<>), mockResolver.Object);

            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))),   Times.Never);
            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<>))), Times.Never);

            for (int i = 0; i < 2; i++)
            {
                Injector.Get<IInterface_3<string>>();

                mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))),         Times.Never);
                mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<string>))), Times.Never);
                mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<>))),       Times.Once);
            }
        }
    }
}
