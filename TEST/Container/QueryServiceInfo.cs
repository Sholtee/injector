/********************************************************************************
* QueryServiceInfo.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    [TestFixture]
    public sealed partial class ContainerTests
    {
        [Test]
        public void Container_QueryServiceInfo_ShouldThrowOnNonRegisteredService()
        {
            Assert.Throws<ServiceNotFoundException>(() => Container.QueryServiceInfo<IDisposable>());
        }

        [Test]
        public void Container_QueryServiceInfo_ShouldWorkWithGenericTypes()
        {
            Container.Service(typeof(IInterface_3<>), typeof(Implementation_3<>));

            IServiceInfo info = Container.QueryServiceInfo(typeof(IInterface_3<>));
            Assert.That(info.IsService);

            IServiceInfo anotherInfo = Container.QueryServiceInfo<IInterface_3<string>>();
            Assert.That(anotherInfo.IsService);

            Assert.AreNotSame(info, anotherInfo);
        }
    }
}
