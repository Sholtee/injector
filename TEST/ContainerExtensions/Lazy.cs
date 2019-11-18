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
    using Internals;
    
    public partial class ContainerTestsBase<TContainer>
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

            AbstractServiceEntry entry = Container.Get<IInterface_1>(QueryModes.ThrowOnError);

            Assert.That(entry.IsService());
            Assert.That(entry.IsLazy());
            Assert.False(entry.IsFactory());
            Assert.False(entry.IsInstance());

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
                .Returns(typeof(Implementation_1_No_Dep));
            mockResolver
                .Setup(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(true);

            Container.Lazy<IInterface_1>(mockResolver.Object, lifetime);

            //
            // Az elso Get()-eleskor kell hivja a rendszer a resolver-t
            //

            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Never);
            mockResolver.Verify(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Once);

            _ = Container.Get<IInterface_1>().Implementation;
            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Once);

            _ = Container.Get<IInterface_1>().Implementation;
            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Once);
        }

        [Test]
        public void Container_Lazy_ShouldHandleOpenGenericTypes()
        {
            var mockResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<>))))
                .Returns(typeof(Implementation_3_IInterface_1_Dependant<>));
            mockResolver
                .Setup(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_3<>))))
                .Returns(true);

            Container.Lazy(typeof(IInterface_3<>), mockResolver.Object);

            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<>))),  Times.Never);
            mockResolver.Verify(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_3<>))), Times.Once);

            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(new TransientServiceEntry(typeof(IInterface_3<int>), null, typeof(Implementation_3_IInterface_1_Dependant<int>), Container), Container.Get<IInterface_3<int>>(QueryModes.AllowSpecialization));

                mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<string>))),  Times.Never);
                mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_3<>))),        Times.Once);
            }
        }

        [Test]
        public void Container_Lazy_ShouldHandleNamedServices()
        {
            var mockResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(typeof(Implementation_1_No_Dep));
            mockResolver
                .Setup(r => r.Supports(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(true);

            Container.Lazy<IInterface_1>("svc1", mockResolver.Object);

            Assert.IsNull(Container.Get<IInterface_1>());
            Assert.IsNull(Container.Get<IInterface_1>("invalidname"));
            Assert.AreEqual(new TransientServiceEntry(typeof(IInterface_1), "svc1", mockResolver.Object, Container), Container.Get<IInterface_1>("svc1"));
        }
    }
}
