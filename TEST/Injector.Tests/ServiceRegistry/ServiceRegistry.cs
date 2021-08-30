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
        public ServiceRegistryBase Registry { get; set; }

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
            Registry = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Array.Empty<AbstractServiceEntry>(), null, int.MaxValue });

            Assert.That(Registry.Children, Is.Empty);

            using (IServiceRegistry child = (ServiceRegistryBase) Activator.CreateInstance(registryType, new object[] { Registry }))
            {
                Assert.That(Registry.Children.Count, Is.EqualTo(1));
                Assert.AreSame(Registry.Children.First(), child);
            }

            Assert.That(Registry.Children, Is.Empty);
        }
    }
}
