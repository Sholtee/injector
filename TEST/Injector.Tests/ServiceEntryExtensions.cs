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
            Assert.False(new TransientServiceEntry(typeof(IDisposable), null, (_, _) => new MyDisposable(), false).IsService());
            Assert.True(new TransientServiceEntry(typeof(IDisposable), null, typeof(MyDisposable), false).IsService());
        }

        [Test]
        public void IsFactory_ShouldDetermineIfTheEntryUsesTheFactoryRecipe()
        {
            Assert.False(new TransientServiceEntry(typeof(IDisposable), null, typeof(MyDisposable), false).IsFactory());
            Assert.True(new TransientServiceEntry(typeof(IDisposable), null, (_, _) => new MyDisposable(), false).IsFactory());
        }

        [Test]
        public void IsInternal_ShouldDetermineIfTheEntryHasAnInternalServiceImplementation([Values(null, "cica", "$cica")] string name)
        {
            Assert.That(new TransientServiceEntry(typeof(IDisposable), name, typeof(MyDisposable), false).IsInternal(), Is.EqualTo(name?.StartsWith("$") is true));
        }

        [Test]
        public void IsInstance_ShouldDetermineIfTheEntryUsesTheInstanceRecipe()
        {
            Assert.False(new TransientServiceEntry(typeof(IDisposable), null, typeof(MyDisposable), false).IsInstance());
            Assert.False(new TransientServiceEntry(typeof(IDisposable), null, (_, _) => new MyDisposable(), false).IsInstance());
            Assert.True(new InstanceServiceEntry(typeof(IDisposable), null, new MyDisposable()).IsInstance());
        }
    }
}
