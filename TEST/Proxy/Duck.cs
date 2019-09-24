/********************************************************************************
* Duck.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

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
            IDisposable result = disposable.Acts().Like<IDisposable>();

            Assert.AreSame(disposable, result);
        }

    }
}
