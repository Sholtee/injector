/********************************************************************************
* MethodInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.InterfaceProxy.Tests
{
    using DI.Internals;

    [TestFixture]
    public sealed class MethodInfoExtensionsTests
    {
        const string CICA = "cica";

        [Test]
        public void Call_ShouldReturn()
        {
            Func<string> toString = CICA.ToString;

            object ret = toString.Method.Call(CICA);
            Assert.That(ret, Is.EqualTo(CICA));
        }

        [Test]
        public void Call_ShouldHandleVoidRetVal()
        {
            var chars = new char[1];

            Action<int, char[], int, int> copyTo = CICA.CopyTo;

            object ret = copyTo.Method.Call(CICA, 3, chars, 0, 1);

            Assert.That(ret, Is.Null);
            Assert.That(chars[0], Is.EqualTo('a'));
        }

        [Test]
        public void Call_ShouldHandleParameters()
        {
            Func<string, int> indexOf = CICA.IndexOf;

            object ret = indexOf.Method.Call(CICA, "a");
            Assert.That(ret, Is.EqualTo(3));
        }

        [Test]
        public void Call_ShouldCache()
        {
            Func<string> toString = CICA.ToString;
            toString.Method.Call(CICA);
    
            Assert.That(MethodInfoExtensions.MethodRegistered(toString.Method));
            Assert.That(MethodInfoExtensions.MethodRegistered(((Func<string>) (CICA).ToString).Method));
            Assert.That(MethodInfoExtensions.MethodRegistered(typeof(string).GetMethod("ToString", new Type[0])));
            Assert.That(MethodInfoExtensions.MethodRegistered(typeof(object).GetMethod("ToString")), Is.False);
        }
    }
}
