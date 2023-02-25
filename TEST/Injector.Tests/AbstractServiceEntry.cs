/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Interfaces.Tests
{
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
    }
}
