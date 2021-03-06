﻿/********************************************************************************
* ServiceEntry.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;
    using System.Collections.Generic;

    [TestFixture]
    public class ServiceEntryTests
    {
        [Test]
        public void Equals_ShouldDoWhatItsNameSuggests()
        {
            var dummyContainer = new Mock<IServiceContainer>(MockBehavior.Strict).Object;
            using (var entry = new AbstractServiceEntry(typeof(IDisposable), null, dummyContainer))
            {
                Assert.That(entry.Equals(entry));
                Assert.That(entry.Equals(new AbstractServiceEntry(typeof(IDisposable), null, dummyContainer)));
                Assert.That(entry.Equals(null), Is.False);
                Assert.That(entry.Equals(new AbstractServiceEntry(typeof(IDisposable), "cica", dummyContainer)), Is.False);
                Assert.That(entry.Equals(new AbstractServiceEntry(typeof(IDisposable), null, new Mock<IServiceContainer>(MockBehavior.Strict).Object)), Is.False);
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
            public MyServiceEntry(Type iface) : base(iface, null, new Mock<IServiceContainer>(MockBehavior.Strict).Object) 
            {
                Instances = new[] { new ServiceReference(this, new Mock<IInjector>().Object) };
            }

            public override IReadOnlyCollection<IServiceReference> Instances { get; }
        }
    }
}
