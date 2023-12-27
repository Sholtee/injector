/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Internals;

    [TestFixture]
    public sealed class ServiceEntryExtensionsTests
    {
        [Test]
        public void IsService_ShouldDetermineIfTheEntryUsesTheServiceRecipe()
        {
            Assert.False(new TransientServiceEntry(typeof(IDisposable), null, (_, _) => new MyDisposable(), ServiceOptions.Default).IsService());
            Assert.True(new TransientServiceEntry(typeof(IDisposable), null, typeof(MyDisposable), ServiceOptions.Default).IsService());
        }

        [Test]
        public void IsService_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => Interfaces.ServiceEntryExtensions.IsService(null));
        }

        [Test]
        public void IsFactory_ShouldDetermineIfTheEntryUsesTheFactoryRecipe()
        {
            Assert.False(new TransientServiceEntry(typeof(IDisposable), null, typeof(MyDisposable), ServiceOptions.Default).IsFactory());
            Assert.True(new TransientServiceEntry(typeof(IDisposable), null, (_, _) => new MyDisposable(), ServiceOptions.Default).IsFactory());
        }

        [Test]
        public void IsFactory_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => Interfaces.ServiceEntryExtensions.IsFactory(null));
        }

        [Test]
        public void IsInstance_ShouldDetermineIfTheEntryUsesTheInstanceRecipe()
        {
            Assert.False(new TransientServiceEntry(typeof(IDisposable), null, typeof(MyDisposable), ServiceOptions.Default).IsInstance());
            Assert.False(new TransientServiceEntry(typeof(IDisposable), null, (_, _) => new MyDisposable(), ServiceOptions.Default).IsInstance());
            Assert.True(new InstanceServiceEntry(typeof(IDisposable), null, new MyDisposable(), ServiceOptions.Default).IsInstance());
        }


        [Test]
        public void IsInstance_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => ServiceEntryAdvancedExtensions.IsInstance(null));
        }
    }
}
