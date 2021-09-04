/********************************************************************************
* TestBase.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

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
                yield return Lifetime.Pooled.WithCapacity(4);
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

        public static IEnumerable<Lifetime> ContainerControlledLifetimes
        {
            get
            {
                yield return Lifetime.Pooled;
                yield return Lifetime.Singleton;
            }
        }
    }
}
