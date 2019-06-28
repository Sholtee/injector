/********************************************************************************
* ConfigFromAssembly.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Container.Setup.Tests
{
    using Annotations;

    [TestFixture]
    public sealed class ConfigFromAssembly
    {
        private interface IGenericService<T>
        {
        }

        [Service(typeof(IGenericService<>), Lifetime.Singleton)]
        private class GenericService<T> : IGenericService<T>
        {
        }

        [Test]
        public void Setup_ShouldHandleGenericTypes()
        {
            var mockContainer = new Mock<IServiceContainer>(MockBehavior.Strict);
            mockContainer
                .Setup(i => i.Service(It.IsAny<Type>(), It.IsAny<Type>(), It.IsAny<Lifetime>()))
                .Returns(mockContainer.Object);

            mockContainer.Object.Setup(typeof(GenericService<>).Assembly);

            mockContainer.Verify(i => i.Service(It.Is<Type>(t => t == typeof(IGenericService<>)), It.Is<Type>(t => t == typeof(GenericService<>)), It.Is<Lifetime>(lt => lt == Lifetime.Singleton)), Times.Once);
        }
    }
}
