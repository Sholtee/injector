/********************************************************************************
* ServiceReference.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Internals;

    [TestFixture]
    public sealed class ServiceReferenceTests
    {
        [Test]
        public void ServiceReference_ShouldManageTheReferenceCounts()
        {
            var target = new Disposable();
            var reference = new ServiceReference(target, Array.Empty<ServiceReference>());

            Assert.That(reference.RefCount, Is.EqualTo(1));

            var svc = new ServiceReference(new object(), new[] { reference });
            Assert.That(reference.RefCount, Is.EqualTo(2));
            
            svc.Release();

            Assert.That(svc.Disposed);
            Assert.That(reference.RefCount, Is.EqualTo(1));

            reference.Release();

            Assert.That(reference.Disposed);
            Assert.That(target.Disposed);
        }
    }
}
