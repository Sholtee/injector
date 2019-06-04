/********************************************************************************
* Lock.cs                                                                       *
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
        public void Injector_LockShouldLockTheCurrentInstance()
        {
            Assert.That(Injector.Locked, Is.False);
            Injector
                .Service<IInterface_1, Implementation_1>()
                .Lock();

            Assert.That(Injector.Locked, Is.True);
            Assert.Throws<InvalidOperationException>(() => Injector.Service<IInterface_2, Implementation_2>(), Resources.LOCKED);

            IInjector child = null;
            Assert.DoesNotThrow(() => child = Injector.CreateChild());

            using (child)
            {
                Assert.DoesNotThrow(() => child.Service<IInterface_2, Implementation_2>());
            }
        }
    }
}
