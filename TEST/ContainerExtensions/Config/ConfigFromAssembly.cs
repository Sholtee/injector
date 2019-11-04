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

            mockContainer.Verify(i => i.Add(It.Is<ScopedServiceEntry>(se => se.Equals(new ScopedServiceEntry(typeof(IGenericService<>), null, typeof(GenericService<>), mockContainer.Object)))), Times.Once);
        }

        [Service(typeof(IDisposable), "svc1")]
        private class MyDisposable_1 : IDisposable
        {
            void IDisposable.Dispose()
            {
            }
        }

        [Service(typeof(IDisposable), "svc2")]
        private class MyDisposable_2 : IDisposable
        {
            void IDisposable.Dispose()
            {
            }
        }

        [Test]
        public void Setup_ShouldHandleNamedServices() 
        {
            var mockContainer = new Mock<IServiceContainer>(MockBehavior.Strict);
            mockContainer
                .Setup(i => i.Add(It.IsAny<AbstractServiceEntry>()))
                .Returns(mockContainer.Object);

            mockContainer.Object.Setup(typeof(GenericService<>).Assembly());

            mockContainer.Verify(i => i.Add(It.Is<TransientServiceEntry>(se => se.Equals(new TransientServiceEntry(typeof(IDisposable), "svc1", typeof(MyDisposable_1), mockContainer.Object)))), Times.Once);
            mockContainer.Verify(i => i.Add(It.Is<TransientServiceEntry>(se => se.Equals(new TransientServiceEntry(typeof(IDisposable), "svc2", typeof(MyDisposable_2), mockContainer.Object)))), Times.Once);
        }
    }
}
