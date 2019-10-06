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
    using Properties;
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
        public void Like_ShouldGenerateAProxy()
        {
            var mock = new Mock<MyDisposable>(MockBehavior.Strict);
            mock.Setup(x => x.Dispose());

            using (IDisposable disposable = mock.Object.Act().Like<IDisposable>())
            {
                Assert.That(disposable, Is.Not.Null);
                Assert.AreNotSame(mock.Object, disposable);

                mock.Verify(x => x.Dispose(), Times.Never);
            }

            mock.Verify(x => x.Dispose(), Times.Once);
        }

        [Test]
        public void Like_ShouldWorkWithStructs()
        {
            var s = MyDisposableStruct.Create();

            using (IDisposable disposable = s.Act().Like<IDisposable>())
            {
                Assert.That(disposable, Is.Not.Null);
            }

            Assert.That(s.Disposed.Value, Is.True);
        }

        public abstract class MyDisposable
        {
            public abstract void Dispose();
        }

        public class Reference<T> where T : struct
        {
            public T Value { get; set; }
        }

        public struct MyDisposableStruct
        {
            public static MyDisposableStruct Create() => new MyDisposableStruct
            {
                Disposed = new Reference<bool>()
            };
            public Reference<bool> Disposed;
            public void Dispose() => Disposed.Value = true;
        }

        [Test]
        public void Like_ShouldValidate()
        {
            Assert.Throws<InvalidOperationException>(() => this.Act().Like<List>(), Resources.NOT_AN_INTERFACE);
            Assert.Throws<InvalidOperationException>(() => this.Act().Like<IPrivateInterface>());
            Assert.Throws<InvalidOperationException>(() => new PrivateClass().Act().Like<IDisposable>());
            Assert.Throws<MissingMethodException>(() => this.Act().Like<IDisposable>());
        }

        private interface IPrivateInterface
        {            
        }

        private class PrivateClass
        {
        }
    }
}
