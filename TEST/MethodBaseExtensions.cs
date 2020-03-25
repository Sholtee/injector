/********************************************************************************
* MethodBaseExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public sealed class MethodBaseExtensions
    {
        private static bool Static() => true;
        private static bool Static(bool b) => b;

        public static IEnumerable<(MethodBase Method, Type Type)> ParameterlessMethods
        {
            get 
            {
                yield return (typeof(List<string>).GetConstructor(new Type[0]), typeof(List<string>));
                yield return (MethodInfoExtractor.Extract<MethodBaseExtensions>(e => Static()), typeof(bool));
            }
        }

        [TestCaseSource(nameof(ParameterlessMethods))]
        public void Call_ShouldReturn((MethodBase Method, Type Type) para) 
            => Assert.That(para.Method.Call(), Is.InstanceOf(para.Type));

        public static IEnumerable<(MethodBase Method, object Param, Type Type)> ParameterfulMethods
        {
            get
            {
                yield return (typeof(List<string>).GetConstructor(new[] { typeof(int)}), 10, typeof(List<string>));
                yield return (MethodInfoExtractor.Extract<MethodBaseExtensions>(e => Static(true)), true, typeof(bool));
            }
        }

        [TestCaseSource(nameof(ParameterfulMethods))]
        public void Call_ShouldHandleParameters((MethodBase Method, object Param, Type Type) para)
            => Assert.That(para.Method.Call(para.Param), Is.InstanceOf(para.Type));

        [Test]
        public void ToDelegate_ShouldCache()
        {
            Assert.AreSame(typeof(List<string>).GetConstructor(new Type[0]).ToDelegate(), typeof(List<string>).GetConstructor(new Type[0]).ToDelegate());
            Assert.That(typeof(List<string>).GetConstructor(new Type[0]).ToDelegate(), Is.Not.SameAs(typeof(List<string>).GetConstructor(new []{typeof(int)}).ToDelegate()));
        }
    }
}
