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

    [TestFixture]
    public sealed class TypeExtensionsTests
    {
        [Test]
        public void CreateInstance_ShouldInstantiate() =>
            Assert.That(typeof(List<int>).CreateInstance(Array.Empty<Type>()), Is.InstanceOf<List<int>>());

        [Test]
        public void CreateInstance_ShouldPassCtorParameters() =>
            Assert.That(((List<int>)typeof(List<int>).CreateInstance(new[] { typeof(int) }, 1)).Capacity, Is.EqualTo(1));

        [Test]
        public void CreateInstance_ShouldThrowIfConstructorCanNotBeFound() =>
            Assert.Throws<ArgumentException>(() => typeof(List<int>).CreateInstance(new[] { typeof(string) }, "cica"), Resources.CONSTRUCTOR_NOT_FOUND);

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
                yield return ProxyFactory.GenerateProxyType(typeof(IDisposable), typeof(InterceptorHavingMultipleCtors<IDisposable>));
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
