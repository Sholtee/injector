/********************************************************************************
* ConstructorInfoExtensions.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
#if NETCOREAPP1_0 || NETCOREAPP1_1
using System.Reflection;
#endif

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public sealed class ConstructorInfoExtensionsTests // TODO: igazabol ebben vannak a TypeExtensions tesztjei is, azt ki kene szervezni kulon fajlba
    {
        [Test]
        public void CreateInstance_ShouldReturn()
        {
            object lst = typeof(List<string>).CreateInstance(new Type[0]); // a CreateInstance egy shortcut a Call()-ra

            Assert.That(lst, Is.InstanceOf<List<string>>());
        }

        [Test]
        public void CreateInstance_ShouldHandleParameters()
        {
            object lst = typeof(List<string>).CreateInstance(new []{typeof(int)}, 10);

            Assert.That(lst, Is.InstanceOf<List<string>>());
            Assert.That(((List<string>) lst).Capacity, Is.EqualTo(10));
        }

        [Test]
        public void CreateInstance_ShouldThrowIfConstructorCouldNotBeFound() =>
            Assert.Throws<ArgumentException>(() => typeof(object).CreateInstance(new[] { typeof(int) }));      

        [Test]
        public void ToDelegate_ShouldCache()
        {
            Assert.AreSame(typeof(List<string>).GetConstructor(new Type[0]).ToDelegate(), typeof(List<string>).GetConstructor(new Type[0]).ToDelegate());
            Assert.That(typeof(List<string>).GetConstructor(new Type[0]).ToDelegate(), Is.Not.SameAs(typeof(List<string>).GetConstructor(new []{typeof(int)}).ToDelegate()));
        }
    }
}
