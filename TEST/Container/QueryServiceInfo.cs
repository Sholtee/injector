/********************************************************************************
* QueryServiceInfo.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class ContainerTests
    {
        [Test]
        public void Container_QueryServiceInfo_ShouldThrowOnNonRegisteredService()
        {
            Assert.Throws<NotSupportedException>(() => Container.QueryServiceInfo<IDisposable>(), string.Format(Resources.DEPENDENCY_NOT_FOUND, typeof(IDisposable)));
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
