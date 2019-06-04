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
        public void ToDelegate_ShouldCache()
        {
            Func<string> toString = CICA.ToString;
            Assert.AreSame(toString.Method.ToDelegate(), toString.Method.ToDelegate());
            Assert.AreSame(((Func<string>)(CICA).ToString).Method.ToDelegate(), toString.Method.ToDelegate());
            Assert.AreSame(typeof(string).GetMethod("ToString", new Type[0]).ToDelegate(), toString.Method.ToDelegate());
            Assert.That(typeof(object).GetMethod("ToString").ToDelegate(), Is.Not.SameAs(toString.Method.ToDelegate()));
        }
    }
}
