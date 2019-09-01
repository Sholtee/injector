/********************************************************************************
* Lazy.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class ContainerTests
    {
        [Test]
        public void Container_Lazy_ShouldBeAService()
        {
            var mockTypeResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockTypeResolver.Setup(i => i.Resolve(It.IsAny<Type>()));
            mockTypeResolver
                .Setup(r => r.Supports(It.IsAny<Type>()))
                .Returns(true);

            Container.Lazy<IInterface_1>(mockTypeResolver.Object);

            IServiceInfo serviceInfo = Container.QueryServiceInfo<IInterface_1>();

            Assert.That(serviceInfo.IsService());
            Assert.That(serviceInfo.IsLazy());
            Assert.False(serviceInfo.IsFactory());
            Assert.False(serviceInfo.IsInstance());

            mockTypeResolver.Verify(i => i.Resolve(It.IsAny<Type>()), Times.Never);
        }

        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Scoped)]
        [TestCase(Lifetime.Singleton)]
        public void Container_Lazy_ShouldCallTheResolverOnRequest(Lifetime lifetime)
        {
            var mockResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(typeof(Implementation_1));
            mockResolver
                .Setup(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(true);

            Container.Lazy<IInterface_1>(mockResolver.Object, lifetime);

            //
            // Az elso Get()-eleskor kell hivja a rendszer a resolver-t
            //

            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Never);
            mockResolver.Verify(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Once);

            using (IInjector injector = Container.CreateInjector())
            {
                var instance = injector.Get<IInterface_1>();
                mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Once);

                Assert.That(instance, Is.InstanceOf<Implementation_1>());

                injector.Get<IInterface_1>();
                mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Once);
            }
        }

        [Test]
        public void Container_Lazy_ShouldBeTypeChecked()
        {
            var mockResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(typeof(Implementation_2));
            mockResolver
                .Setup(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(true);

            Container.Lazy<IInterface_1>(mockResolver.Object);

            using (IInjector injector = Container.CreateInjector())
            {
                Assert.Throws<InvalidOperationException>(() => injector.Get<IInterface_1>(), string.Format(Resources.NOT_ASSIGNABLE, typeof(IInterface_1), typeof(object)));
            }         
        }

        [Test]
        public void Container_Lazy_ShouldHandleOpenGenericTypes()
        {
            var mockResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<>))))
                .Returns(typeof(Implementation_3<>));
            mockResolver
                .Setup(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_3<>))))
                .Returns(true);

            Container
                .Service<IInterface_1, Implementation_1>()
                .Lazy(typeof(IInterface_3<>), mockResolver.Object);

            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))),    Times.Never);
            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<>))),  Times.Never);
            mockResolver.Verify(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_3<>))), Times.Once);

            for (int i = 0; i < 2; i++)
            {
                using (IInjector injector = Container.CreateInjector())
                {
                    injector.Get<IInterface_3<string>>();
                }
                
                mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))),          Times.Never);
                mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<string>))),  Times.Never);
                mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<>))),        Times.Once);
            }
        }
    }
}
