using System;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests.InterfaceProxy
{
    [TestFixture]
    public sealed class MethodInfoExtensionsTests
    {
        const string CICA = "cica";

        [Test]
        public void FastInvoke_ShouldReturn()
        {
            Func<string> toString = CICA.ToString;

            object ret = toString.Method.FastInvoke(CICA);
            Assert.That(ret, Is.EqualTo(CICA));
        }

        [Test]
        public void FastInvoke_ShouldHandleVoidRetVal()
        {
            var chars = new char[1];

            Action<int, char[], int, int> copyTo = CICA.CopyTo;

            object ret = copyTo.Method.FastInvoke(CICA, 3, chars, 0, 1);

            Assert.That(ret, Is.Null);
            Assert.That(chars[0], Is.EqualTo('a'));
        }

        [Test]
        public void FastInvoke_ShouldHandleParameters()
        {
            Func<string, int> indexOf = CICA.IndexOf;

            object ret = indexOf.Method.FastInvoke(CICA, "a");
            Assert.That(ret, Is.EqualTo(3));
        }
    }
}
