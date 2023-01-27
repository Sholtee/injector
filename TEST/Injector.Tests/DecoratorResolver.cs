/********************************************************************************
* DecoratorResolver.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;

    [TestFixture]
    public class DecoratorResolverTests
    {
        private sealed class AspectExposingInvalidInterceptor : AspectAttribute
        {
            public override Type UnderlyingInterceptor { get; } = typeof(object);
        }

        public interface IMyService { }

        [AspectExposingInvalidInterceptor]
        public class MyServiceUsingInvalidAspect : IMyService
        {
        }

        [Test]
        public void ResolveForAspects_ShouldThrowOnInvalidInterceptor() =>
            Assert.Throws<InvalidOperationException>(() => DecoratorResolver.ResolveForAspects(typeof(IMyService), typeof(MyServiceUsingInvalidAspect), ProxyEngine.Instance));

        private sealed class MyAspect : AspectAttribute
        {
            public sealed class MyInterceptor : IInterfaceInterceptor
            {
                public MyInterceptor(IList dependency) => Dependency = dependency;

                public IList Dependency { get; }

                public object Invoke(IInvocationContext context, InvokeInterceptorDelegate callNext) => callNext();
            }

            public override Type UnderlyingInterceptor { get; } = typeof(MyInterceptor);
        }

        [MyAspect]
        public class MyService : IMyService
        {
        }

        [Test]
        public void ResolveForAspects_ShouldInstantiateTheInterceptors()
        {
            Mock<IInjector> mockInjector = new(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList), null))
                .Returns(new List<object>());

            DecoratorDelegate decorator = DecoratorResolver
                .ResolveForAspects(typeof(IMyService), typeof(MyService), ProxyEngine.Instance)
                .Compile();
            AspectAggregator<IMyService, MyService> proxy = (AspectAggregator<IMyService, MyService>) decorator
            (
                mockInjector.Object, typeof(IMyService), new MyService()
            );

            Assert.That(proxy.Interceptors.Count, Is.EqualTo(1));
            Assert.That(proxy.Interceptors[0], Is.InstanceOf<MyAspect.MyInterceptor>());
            mockInjector.Verify(i => i.Get(typeof(IList), null), Times.Once);
        }
    }
}
