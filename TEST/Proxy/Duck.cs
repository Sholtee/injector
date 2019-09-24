﻿/********************************************************************************
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

        [Test]
        public void Like_ShouldValidate()
        {
            Assert.Throws<ArgumentException>(() => this.Act().Like<object>(), Resources.NOT_AN_INTERFACE);
            Assert.Throws<InvalidOperationException>(() => this.Act().Like<IPrivateInterface>());
            Assert.Throws<InvalidOperationException>(() => new PrivateClass().Act().Like<IDisposable>());
            Assert.Throws<MissingMethodException>(() => this.Act().Like<IDisposable>());
        }

        internal interface IPrivateInterface
        {            
        }

        internal class PrivateClass
        {
        }
    }
}
