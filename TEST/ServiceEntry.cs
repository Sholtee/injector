/********************************************************************************
* ServiceEntry.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public class ServiceEntryTests
    {
        [Test]
        public void Equals_ShouldDoWhatItsNameSuggests() 
        {
            using (var entry = new AbstractServiceEntry(typeof(IDisposable), null))
            {
                Assert.That(entry.Equals(entry));
                Assert.That(entry.Equals(new AbstractServiceEntry(typeof(IDisposable), null)));
                Assert.That(entry.Equals(null), Is.False);
                Assert.That(entry.Equals(new AbstractServiceEntry(typeof(IDisposable), "cica")), Is.False);
            }
        }

        [Test]
        public void Dispose_ShouldDecerementTheReferenceCountOfTheInstance() 
        {
            var entry = new MyServiceEntry(typeof(IDisposable));

            Assert.That(entry.Instance.RefCount, Is.EqualTo(1));
            entry.Dispose();
            Assert.That(entry.Instance.RefCount, Is.EqualTo(0));  
        }

        [Test]
        public async Task DisposeAsync_ShouldDecerementTheReferenceCountOfTheInstance()
        {
            var entry = new MyServiceEntry(typeof(IDisposable));

            Assert.That(entry.Instance.RefCount, Is.EqualTo(1));
            await entry.DisposeAsync();
            Assert.That(entry.Instance.RefCount, Is.EqualTo(0));
        }

        private class MyServiceEntry : AbstractServiceEntry 
        {
            public MyServiceEntry(Type iface) : base(iface, null) 
            {
                Instance = new ServiceReference(this, new Mock<IInjector>().Object);
            }
        }
    }
}
