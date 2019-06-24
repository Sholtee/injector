/********************************************************************************
* CreateChild.cs                                                                *
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
        public void Container_ChildShouldInheritTheParentEntries()
        {
            IServiceContainer child = Container
                .Service<IInterface_1, Implementation_1>()
                .CreateChild();

            Assert.That(child.QueryServiceInfo<IInterface_1>().Implementation, Is.EqualTo(typeof(Implementation_1)));
        }

        [Test]
        public void Container_ChildShouldInheritTheProxies()
        {
            Container.Service<IInterface_1, Implementation_1>();

            Func<IInjector, Type, object> 
                originalFactory = Container.QueryServiceInfo<IInterface_1>().Factory,
                proxiedFactory = Container
                    .Proxy<IInterface_1>((me, val) => new DecoratedImplementation_1())
                    .QueryServiceInfo<IInterface_1>()
                    .Factory;

            Assert.AreNotSame(originalFactory, proxiedFactory);

            IServiceContainer child = Container.CreateChild();
            Assert.AreSame(proxiedFactory, child.QueryServiceInfo<IInterface_1>().Factory);
        }

        [Test]
        public void Container_ChildShouldBeIndependentFromTheParent()
        {
            IServiceContainer child = Container
                .CreateChild()
                .Service<IInterface_1, Implementation_1>();

            Assert.Throws<NotSupportedException>(() => Container.QueryServiceInfo<IInterface_1>(), string.Format(Resources.DEPENDENCY_NOT_FOUND, typeof(IInterface_1)));
            Assert.DoesNotThrow(() => child.QueryServiceInfo<IInterface_1>());
        }


        [Test]
        public void Container_CreateChild_ShouldNotTriggerTheTypeResolver()
        {
            var mockTypeResolver = new Mock<ITypeResolver>(MockBehavior.Strict);
            mockTypeResolver.Setup(i => i.Resolve(It.IsAny<Type>()));

            Container.Lazy<IInterface_1>(mockTypeResolver.Object);

            using (Container.CreateChild())
            {
            }

            mockTypeResolver.Verify(i => i.Resolve(It.IsAny<Type>()), Times.Never);
        }
    }
}
