/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Interfaces.Tests
{
    using DI.Tests;
    using Internals;
    using Properties;

    [TestFixture]
    public sealed class AbstractServiceEntryTests
    {
        private AbstractServiceEntry Entry { get; set; }

        [OneTimeSetUp]
        public void SetupFixture() => Entry = new Mock<AbstractServiceEntry>(typeof(IDisposable), null, null, null, null, null) { CallBase = true }.Object;

        [Test]
        public void Specialize_ShouldNotBeSupportedByDefault()
        {
            Assert.Throws<NotSupportedException>(() => Entry.Specialize());
        }

        [Test]
        public void SetValidated_ShouldNotBeSupportedByDefault()
        {
            Assert.Throws<NotSupportedException>(() => Entry.UpdateState(ServiceEntryStates.Default));
        }

        [Test]
        public void Decorate_ShouldNotBeSupportedByDefault()
        {
            Assert.Throws<NotSupportedException>(() => Entry.Decorate(null));
        }

        [Test]
        public void Build_ShouldNotBeSupportedByDefault()
        {
            Assert.Throws<NotSupportedException>(() => Entry.Build(null, null));
        }

        [Test]
        public void CreateLifetimeManager_ShouldNotBeSupportedByDefault()
        {
            Assert.Throws<NotSupportedException>(() => Entry.CreateLifetimeManager(null, null, null));
        }

        [Test]
        public void ToString_ShouldStringify([Values(true, false)] bool shortForm)
        {
            AbstractServiceEntry entry = new ScopedServiceEntry(typeof(MyService), "key", typeof(MyService), ServiceOptions.Default);

            string expected = "Solti.Utils.DI.Tests.MyService:key";
            if (!shortForm)
                expected += " - Lifetime: Scoped - Implementation: Solti.Utils.DI.Tests.MyService - Factory: (scope, type) => { ... }";

            Assert.That(entry.ToString(shortForm), Is.EqualTo(expected));
        }

        [Test]
        public void Ctor_ShouldCheckTheImplementation()
        {
            Assert.That(Assert.Throws<TargetInvocationException>(() => _ = new Mock<AbstractServiceEntry>(typeof(IDisposable), null, typeof(int), null, null, null).Object).InnerException.Message, Does.StartWith(Resources.NOT_A_CLASS));
            Assert.That(Assert.Throws<TargetInvocationException>(() => _ = new Mock<AbstractServiceEntry>(typeof(IDisposable), null, typeof(AbstractServiceEntry), null, null, null).Object).InnerException.Message, Does.StartWith(Resources.ABSTRACT_CLASS));
        }

        [Test]
        public void Ctor_ShouldBeNullChecked() =>
            Assert.That(Assert.Throws<TargetInvocationException>(() => _ = new Mock<AbstractServiceEntry>(null, null, null, null, null, null).Object).InnerException, Is.InstanceOf<ArgumentNullException>());
    }
}
