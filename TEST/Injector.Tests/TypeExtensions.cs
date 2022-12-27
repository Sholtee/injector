/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;
    using Properties;

    [TestFixture]
    public sealed class TypeExtensionsTests
    {
        public class MyClassHavingMultipleCtors
        {
            [ServiceActivator]
            public MyClassHavingMultipleCtors() { }
            public MyClassHavingMultipleCtors(int cica) { }
        }

        public static IEnumerable<Type> TestTypesHavingMultipleCtors
        {
            get
            {
                yield return typeof(MyClassHavingMultipleCtors);
            }
        }

        [TestCaseSource(nameof(TestTypesHavingMultipleCtors))]
        public void GetApplicableConstructor_ShouldTakeServiceActivatorAttributeIntoAccount(Type type) =>
            Assert.DoesNotThrow(() => type.GetApplicableConstructor());

        public class MyClassHavingSingleCtor
        {
            public MyClassHavingSingleCtor(IDisposable param) { }
        }

        [Test]
        public void GetApplicableConstructor_ShouldReturnSingleCtors() =>
            Assert.DoesNotThrow(() => typeof(MyClassHavingSingleCtor).GetApplicableConstructor());


        public class MyClassHavingMultipleCtorsWithOutServiceActivator
        {
            public MyClassHavingMultipleCtorsWithOutServiceActivator() { }
            public MyClassHavingMultipleCtorsWithOutServiceActivator(int cica) { }
        }

        [Test]
        public void GetApplicableConstructor_ShouldThrowOnAmbiguousResult() =>
            Assert.Throws<NotSupportedException>(() => typeof(MyClassHavingMultipleCtorsWithOutServiceActivator).GetApplicableConstructor(), Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED);
    }
}
