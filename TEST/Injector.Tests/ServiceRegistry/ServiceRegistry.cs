/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;

    [TestFixture]
    internal partial class ServiceRegistryTests
    {
        public ServiceRegistry Registry { get; set; }

        public static IEnumerable<ResolverBuilder> ResolverBuilders
        {
            get
            {
                yield return ResolverBuilder.ChainedDelegates;
                yield return ResolverBuilder.CompiledExpression;
            }
        }

        [TearDown]
        public void TearDown() => Registry?.Dispose();

        [Test]
        public void Ctor_ShouldThrowIfTheEntryCanNotBeSpecialized()
        {
            AbstractServiceEntry entry = new(typeof(IList<>), null, new ServiceContainer());
            Assert.Throws<InvalidOperationException>(() => new ServiceRegistry(new[] { entry }));
        }

        [Test]
        public void Children_ShouldBeUpToDate()
        {
            Registry = new ServiceRegistry(Array.Empty<AbstractServiceEntry>());

            Assert.That(Registry.Children, Is.Empty);

            using (IServiceRegistry child = new  ServiceRegistry(Registry))
            {
                Assert.That(Registry.Children.Count, Is.EqualTo(1));
                Assert.AreSame(Registry.Children.First(), child);
            }

            Assert.That(Registry.Children, Is.Empty);
        }
    }
}
