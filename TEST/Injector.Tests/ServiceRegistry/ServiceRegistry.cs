/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;
    using Properties;

    [TestFixture]
    internal partial class ServiceRegistryTests
    {
        public IServiceRegistry Registry { get; set; }

        public static IEnumerable<ResolverBuilder> ResolverBuilders
        {
            get
            {
                yield return ResolverBuilder.ChainedDelegates;
                yield return ResolverBuilder.CompiledExpression;
                yield return ResolverBuilder.CompiledCode;
            }
        }

        public static IEnumerable<Type> RegistryTypes 
        {
            get 
            {
                yield return typeof(ServiceRegistry);
                yield return typeof(ConcurrentServiceRegistry);
            }
        }

        [TearDown]
        public void TearDown() => Registry?.Dispose();

        [Test]
        public void Children_ShouldBeUpToDate([ValueSource(nameof(RegistryTypes))] Type registryType)
        {
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance), null, CancellationToken.None });

            Assert.That(Registry.DerivedRegistries, Is.Empty);

            using (IServiceRegistry child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry }))
            {
                Assert.That(Registry.DerivedRegistries.Count, Is.EqualTo(1));
                Assert.AreSame(Registry.DerivedRegistries.First(), child);
            }

            Assert.That(Registry.DerivedRegistries, Is.Empty);
        }

        [Test]
        public void Ctor_ShouldThrowOnOverriddenService() =>
            Assert.Throws<ArgumentException>(() => new ScopeFactory(new HashSet<AbstractServiceEntry>(ServiceIdComparer.Instance) { new AbstractServiceEntry(typeof(IInjector), null, null) }, new ScopeOptions()), Resources.BUILT_IN_SERVICE_OVERRIDE);
    }
}
