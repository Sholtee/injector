/********************************************************************************
* MethodInfoExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public sealed class MethodInfoExtensionsTests
    {
        const string CICA = "cica";

        [Test]
        public void Call_ShouldReturn()
        {
            Func<string> toString = CICA.ToString;

            object ret = toString.GetMethodInfo().Call(CICA);
            Assert.That(ret, Is.EqualTo(CICA));
        }

        [Test]
        public void Call_ShouldHandleVoidRetVal()
        {
            var chars = new char[1];

            Action<int, char[], int, int> copyTo = CICA.CopyTo;

            object ret = copyTo.GetMethodInfo().Call(CICA, 3, chars, 0, 1);

            Assert.That(ret, Is.Null);
            Assert.That(chars[0], Is.EqualTo('a'));
        }

        [Test]
        public void Call_ShouldHandleParameters()
        {
            Func<string, int> indexOf = CICA.IndexOf;

            object ret = indexOf.GetMethodInfo().Call(CICA, "a");
            Assert.That(ret, Is.EqualTo(3));
        }

        [Test]
        public void ToDelegate_ShouldCache()
        {
            Func<string> toString = CICA.ToString;
            Assert.AreSame(toString.GetMethodInfo().ToDelegate(), toString.GetMethodInfo().ToDelegate());
            Assert.AreSame(((Func<string>)(CICA).ToString).GetMethodInfo().ToDelegate(), toString.GetMethodInfo().ToDelegate());
            Assert.AreSame(typeof(string).GetMethod("ToString", new Type[0]).ToDelegate(), toString.GetMethodInfo().ToDelegate());
            Assert.That(typeof(object).GetMethod("ToString").ToDelegate(), Is.Not.SameAs(toString.GetMethodInfo().ToDelegate()));
        }
    }
}
