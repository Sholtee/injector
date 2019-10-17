/********************************************************************************
* ConfigFromAssembly.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Setup.Tests
{
    using Annotations;
    using Internals;

    [TestFixture]
    public sealed class ConfigFromAssembly
    {       
        public interface IGenericService<T>
        {
        }

        [Service(typeof(IGenericService<>), Lifetime.Scoped)]
        public class GenericService<T> : IGenericService<T>
        {
        }

        [Test]
        public void Setup_ShouldHandleGenericTypes()
        {
            var mockContainer = new Mock<IServiceContainer>(MockBehavior.Strict);
            mockContainer
                .Setup(i => i.Add(It.IsAny<AbstractServiceEntry>()))
                .Returns(mockContainer.Object);

            mockContainer.Object.Setup(typeof(GenericService<>).Assembly());

            mockContainer.Verify(i => i.Add(It.Is<ScopedServiceEntry>(se => se.Equals(new ScopedServiceEntry(typeof(IGenericService<>), typeof(GenericService<>), mockContainer.Object)))), Times.Once);
        }
    }
}
