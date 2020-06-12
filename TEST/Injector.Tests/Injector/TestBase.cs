/********************************************************************************
* TestBase.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Tests
{
    using DI.Tests;
    using Interfaces;

    public abstract partial class InjectorTestsBase<TContainer> : TestBase<TContainer> where TContainer : IServiceContainer, new()
    {
    }

    [TestFixture]
    public class InjectorTests : InjectorTestsBase<ServiceContainer>
    {
    }
}
