/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;

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
    }
}
