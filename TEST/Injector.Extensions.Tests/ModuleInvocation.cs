/********************************************************************************
* ModuleInvocation.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Extensions.Tests
{
    using Interfaces;

    [TestFixture]
    public class ModuleInvocationTests
    {
        public interface IService
        {
            int Add(int a, int b);
            void Void();
            [Alias("Cica")]
            void Bar();
        }

        [Test]
        public void ModuleInvocation_ShouldQueryTheService() 
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IService), null))
                .Returns(new Mock<IService>().Object);

            ModuleInvocation invocation = new ModuleInvocationBuilder().Build(typeof(IService));
            invocation.Invoke(mockInjector.Object, nameof(IService), nameof(IService.Add), 1, 2);

            mockInjector.Verify(i => i.Get(It.IsAny<Type>(), null), Times.Once);
        }

        [Test]
        public void ModuleInvocation_ShouldCallTheDesiredMethod() 
        {
            var mockService = new Mock<IService>(MockBehavior.Strict);
            mockService
                .Setup(svc => svc.Add(1, 2))
                .Returns<int, int>((a, b) => a + b);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IService), null))
                .Returns(mockService.Object);

            ModuleInvocation invocation = new ModuleInvocationBuilder().Build(typeof(IService));

            Assert.That(invocation.Invoke(mockInjector.Object, nameof(IService), nameof(IService.Add), 1, 2), Is.EqualTo(3));
            mockService.Verify(svc => svc.Add(1, 2), Times.Once);
        }

        [Test]
        public void ModuleInvocation_ShouldThrowIfTheModuleNotFound() 
        {
            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);

            ModuleInvocation invocation = new ModuleInvocationBuilder().Build(typeof(IService));
            Assert.Throws<ServiceNotFoundException>(() => invocation.Invoke(mockInjector.Object, "Invalid", nameof(IService.Add), 1, 2));
        }

        [Test]
        public void ModuleInvocation_ShouldThrowIfTheMethodNotFound() 
        {
            var mockService = new Mock<IService>(MockBehavior.Strict);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IService), null))
                .Returns(mockService.Object);

            ModuleInvocation invocation = new ModuleInvocationBuilder().Build(typeof(IService));

            Assert.Throws<MissingMethodException>(() => invocation.Invoke(mockInjector.Object, nameof(IService), "Invalid"));
        }

        [Test]
        public void ModuleInvocation_ShouldThrowOnInvalidParameter() 
        {
            var mockService = new Mock<IService>(MockBehavior.Strict);

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IService), null))
                .Returns(mockService.Object);

            ModuleInvocation invocation = new ModuleInvocationBuilder().Build(typeof(IService));

            Assert.Throws<IndexOutOfRangeException>(() => invocation.Invoke(mockInjector.Object, nameof(IService), nameof(IService.Add), 1));
            Assert.Throws<InvalidCastException>(() => invocation.Invoke(mockInjector.Object, nameof(IService), nameof(IService.Add), 1, "2"));
        }

        [Test]
        public void ModuleInvocation_ShouldWorkWithVoidMethods() 
        {
            var mockService = new Mock<IService>(MockBehavior.Strict);
            mockService
                .Setup(svc => svc.Void());

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IService), null))
                .Returns(mockService.Object);

            ModuleInvocation invocation = new ModuleInvocationBuilder().Build(typeof(IService));

            invocation.Invoke(mockInjector.Object, nameof(IService), nameof(IService.Void));
            mockService.Verify(svc => svc.Void(), Times.Once);
        }

        [Test]
        public void ModuleInvocation_ShouldHandleMethodAlias()
        {
            var mockService = new Mock<IService>(MockBehavior.Strict);
            mockService
                .Setup(svc => svc.Bar());

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IService), null))
                .Returns(mockService.Object);

            ModuleInvocation invocation = new ModuleInvocationBuilder().Build(typeof(IService));

            Assert.Throws<MissingMethodException>(() => invocation.Invoke(mockInjector.Object, nameof(IService), nameof(IService.Bar)));
            Assert.DoesNotThrow(() => invocation.Invoke(mockInjector.Object, nameof(IService), "Cica"));
        }
    }
}
