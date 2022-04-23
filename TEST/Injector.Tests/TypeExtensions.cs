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
    using Proxy;
    using Proxy.Generators;

    [TestFixture]
    public sealed class TypeExtensionsTests
    {
        public class InterceptorHavingMultipleCtors<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
        {
            [ServiceActivator]
            public InterceptorHavingMultipleCtors(TInterface target) : base(target) { }

            public InterceptorHavingMultipleCtors() : base(default(TInterface)) { }
        }

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
                yield return ProxyGenerator<IDisposable, InterceptorHavingMultipleCtors<IDisposable>>.GetGeneratedType();
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
