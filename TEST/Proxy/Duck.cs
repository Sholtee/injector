/********************************************************************************
* Duck.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Proxy.Tests
{
    using Internals;

    [TestFixture]
    public sealed class DuckTypingTests
    {
        [Test]
        public void Like_ShouldTryToCastTheTarget()
        {
            var disposable = new Disposable();
            IDisposable result = disposable.Act().Like<IDisposable>();

            Assert.AreSame(disposable, result);
        }

        [Test]
        public void Line_ShouldGenerateAProxy()
        {
            var mock = new Mock<MyDisposable>(MockBehavior.Strict);
            mock.Setup(x => x.Dispose());

            IDisposable disposable = mock.Object.Act().Like<IDisposable>();

            Assert.That(disposable, Is.Not.Null);
            Assert.AreNotSame(mock.Object, disposable);

            mock.Verify(x => x.Dispose(), Times.Never);
            disposable.Dispose();
            mock.Verify(x => x.Dispose(), Times.Once);
        }

        public abstract class MyDisposable
        {
            public abstract void Dispose();
        }
    }
}
