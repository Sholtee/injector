/********************************************************************************
* QueryServiceInfo.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        [Test]
        public void QueryServiceInfo_ShouldThrowOnNonRegisteredService()
        {
            Assert.Throws<NotSupportedException>(() => Injector.QueryServiceInfo<IDisposable>(), string.Format(Resources.DEPENDENCY_NOT_FOUND, typeof(IDisposable)));
        }

        [Test]
        public void QueryServiceInfo_ShouldWorkWithGenericTypes()
        {
            Injector.Service(typeof(IInterface_3<>), typeof(Implementation_3<>));

            IServiceInfo info = Injector.QueryServiceInfo(typeof(IInterface_3<>));
            Assert.That(info.IsService);

            IServiceInfo anotherInfo = Injector.QueryServiceInfo<IInterface_3<string>>();
            Assert.That(anotherInfo.IsService);

            Assert.AreNotSame(info, anotherInfo);
        }
    }
}
