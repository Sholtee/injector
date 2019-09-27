/********************************************************************************
* TestBase.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Tests
{
    using DI.Tests;
    
    public abstract partial class ContainerTestsBase<TContainer>: TestBase<TContainer> where TContainer: IServiceContainer, new()
    {
    }

    [TestFixture]
    public class IServiceContainerExtensionsTests : ContainerTestsBase<ServiceContainer>
    {
    }
}
