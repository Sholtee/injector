/********************************************************************************
* DecoratorResolver.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
            public AspectExposingInvalidInterceptor() : base(typeof(object)) { }
        }

        public interface IMyService { }

        [AspectExposingInvalidInterceptor]
        public class MyServiceUsingInvalidAspect : IMyService
        {
        }

        [Test]
        public void ResolveDecorators_ShouldThrowOnInvalidInterceptor() =>
            Assert.Throws<InvalidOperationException>(() => new TransientServiceEntry(typeof(IMyService), null, typeof(MyServiceUsingInvalidAspect), ServiceOptions.Default).ResolveDecorators().ToList());

        private sealed class MyAspect : AspectAttribute
        {
            public sealed class MyInterceptor : IInterfaceInterceptor
            {
                public MyInterceptor(IList dependency) => Dependency = dependency;

                public IList Dependency { get; }

                public object Invoke(IInvocationContext context, CallNextDelegate<IInvocationContext, object> callNext) => callNext(context);
            }

            public MyAspect() : base(typeof(MyInterceptor)) { }
        }

        [MyAspect]
        public class MyService : IMyService
        {
        }

        [Test]
        public void ResolvedDecorator_ShouldInstantiateTheInterceptor()
        {
            Mock<IInjector> mockInjector = new(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList), null))
                .Returns(new List<object>());

            DecoratorDelegate decorator = new TransientServiceEntry(typeof(IMyService), null, typeof(MyService), ServiceOptions.Default)
                .ResolveDecorators()
                .Single()
                .Compile();
            IInterceptorAggregator proxy = (IInterceptorAggregator) decorator
            (
                mockInjector.Object, typeof(IMyService), new MyService()
            );

            Assert.That(proxy.Interceptors.Count, Is.EqualTo(1));
            Assert.That(proxy.Interceptors[0], Is.InstanceOf<MyAspect.MyInterceptor>());
            mockInjector.Verify(i => i.Get(typeof(IList), null), Times.Once);
        }

        private sealed class MyAspectUsingExplicitArg : AspectAttribute
        {
            public sealed class MyInterceptor : IInterfaceInterceptor
            {
                public MyInterceptor(IList dependency, int dependency2)
                {
                    Dependency = dependency;
                    Dependency2 = dependency2;
                }

                public IList Dependency { get; }

                public int Dependency2 { get; }

                public object Invoke(IInvocationContext context, CallNextDelegate<IInvocationContext, object> callNext) => callNext(context);
            }

            public MyAspectUsingExplicitArg() : base(typeof(MyInterceptor), new { dependency2 = 1986 }) { }
        }

        [MyAspectUsingExplicitArg]
        public class MyService2 : IMyService
        {
        }

        [Test]
        public void ResolvedDecorator_ShouldInstantiateTheInterceptorsUsingTheProvidedExplicitArgs()
        {
            Mock<IInjector> mockInjector = new(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IList), null))
                .Returns(new List<object>());

            DecoratorDelegate decorator = new TransientServiceEntry(typeof(IMyService), null, typeof(MyService2), ServiceOptions.Default)
                .ResolveDecorators()
                .Single()
                .Compile();
            IInterceptorAggregator proxy = (IInterceptorAggregator) decorator
            (
                mockInjector.Object, typeof(IMyService), new MyService2()
            );

            Assert.That(proxy.Interceptors.Count, Is.EqualTo(1));
            Assert.That(proxy.Interceptors[0], Is.InstanceOf<MyAspectUsingExplicitArg.MyInterceptor>());
            Assert.That(((MyAspectUsingExplicitArg.MyInterceptor) proxy.Interceptors[0]).Dependency2, Is.EqualTo(1986));
            mockInjector.Verify(i => i.Get(typeof(IList), null), Times.Once);
        }
    }
}
