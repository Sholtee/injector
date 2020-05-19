/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Primitives.Patterns;
    using Properties;

    [TestFixture]
    class ProducibleServiceEntryTests
    {
        [Test]
        public void Create_ShouldThrowOnUnknownLifetime()
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                Assert.Throws<ArgumentException>(() => ProducibleServiceEntry.Create(null, typeof(IDisposable), null, typeof(Disposable), new ServiceContainer()), Resources.UNKNOWN_LIFETIME);
            }
        }

        [Test]
        public void Create_ShouldCreateTheProperEntry([Values(Lifetime.Transient, Lifetime.Scoped, Lifetime.Singleton)] Lifetime lifetime) 
        {
            using (IServiceContainer container = new ServiceContainer())
            {
                AbstractServiceEntry entry = null;
                
                Assert.DoesNotThrow(() => entry = ProducibleServiceEntry.Create(lifetime, typeof(IDisposable), null, typeof(Disposable), new ServiceContainer()));
                Assert.That(entry.Lifetime, Is.EqualTo(lifetime));
            }
        }
    }
}
