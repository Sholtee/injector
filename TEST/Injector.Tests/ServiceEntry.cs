/********************************************************************************
* ServiceEntry.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;

    [TestFixture]
    public class ServiceEntryTests
    {
        [Test]
        public void Equals_ShouldDoWhatItsNameSuggests()
        {
            using (var entry = new DummyServiceEntry(typeof(IDisposable), null))
            {
                Assert.That(entry.Equals(entry));
                Assert.That(entry.Equals(new DummyServiceEntry(typeof(IDisposable), null)));
                Assert.That(entry.Equals(null), Is.False);
                Assert.That(entry.Equals(new DummyServiceEntry(typeof(IDisposable), "cica")), Is.False);
            }
        }

        [Test]
        public void Dispose_ShouldDecerementTheReferenceCountOfTheInstance() 
        {
            var entry = new MyServiceEntry(typeof(IDisposable));

            Assert.That(entry.Instances.Single().RefCount, Is.EqualTo(1));
            entry.Dispose();
            Assert.That(entry.Instances.Single().RefCount, Is.EqualTo(0));  
        }

        [Test]
        public async Task DisposeAsync_ShouldDecerementTheReferenceCountOfTheInstance()
        {
            var entry = new MyServiceEntry(typeof(IDisposable));

            Assert.That(entry.Instances.Single().RefCount, Is.EqualTo(1));
            await entry.DisposeAsync();
            Assert.That(entry.Instances.Single().RefCount, Is.EqualTo(0));
        }

        private class MyServiceEntry : AbstractServiceEntry 
        {
            public MyServiceEntry(Type iface) : base(iface, null) 
            {
                Instances = new[] { new ServiceReference(this, new Mock<IInjector>().Object) };
            }

            public override IReadOnlyCollection<IServiceReference> Instances { get; }

            public override AbstractServiceEntry CopyTo(IServiceRegistry owner)
            {
                throw new NotImplementedException();
            }

            public override bool SetInstance(IServiceReference serviceReference)
            {
                throw new NotImplementedException();
            }
        }
    }
}
