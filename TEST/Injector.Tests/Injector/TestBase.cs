/********************************************************************************
* TestBase.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using DI.Tests;
    using Interfaces;

    public abstract partial class InjectorTestsBase<TContainer> : TestBase<TContainer> where TContainer : IServiceContainer, new()
    {
        public static IEnumerable<Lifetime> Lifetimes
        {
            get
            {
                yield return Lifetime.Transient;
                yield return Lifetime.Scoped;
                yield return Lifetime.Singleton;
            }
        }

        public static IEnumerable<Lifetime> InjectorControlledLifetimes
        {
            get
            {
                yield return Lifetime.Transient;
                yield return Lifetime.Scoped;
            }
        }
    }

    [TestFixture]
    public class InjectorTests : InjectorTestsBase<ServiceContainer>
    {
    }
}
