﻿/********************************************************************************
* ProxyFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.Proxy.Tests
{
    using DI.Interfaces;
    using DI.Internals;

    using Primitives.Patterns;

    using Proxy.Generators;

    [TestFixture]
    public class ProxyFactoryTests
    {
        [Test]
        public void Create_ShouldWorkWithTypeArguments() 
        {
            Assert.DoesNotThrow(() => ProxyFactory.Create<IList<object>, InterfaceInterceptor<IList<object>>>(target: new List<object>()));
            Assert.DoesNotThrow(() => ProxyFactory.Create<IList<object>, InterfaceInterceptor<IList<object>>>(argTypes: new[] { typeof(IList<object>) }, args: new List<object>()));
            Assert.DoesNotThrow(() => ProxyFactory.Create<IList<object>, InterfaceInterceptor<IList<object>>>(target: new List<object>()));
            Assert.DoesNotThrow(() => ProxyFactory.Create<IList<object>, InterfaceInterceptor<IList<object>>>(args: new[] { new List<object>() }));

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IDisposable), null))
                .Returns(new Disposable());

            Assert.DoesNotThrow(() => ProxyFactory.Create<IList<object>, InterceptorHavingDependency>(target: new List<object>(), injector: mockInjector.Object));

            mockInjector.Verify(i => i.Get(typeof(IDisposable), null), Times.Once);
        }

        public class InterceptorHavingDependency : InterfaceInterceptor<IList<object>>
        {
            public InterceptorHavingDependency(IList<object> target, IDisposable dependency) : base(target)
            {
            }
        }

        [Test]
        public void Create_ShouldWorkWithTypeParameters()
        {
            Assert.DoesNotThrow(() => ProxyFactory.Create(typeof(IList<object>), typeof(InterfaceInterceptor<IList<object>>), target: new List<object>()));
            Assert.DoesNotThrow(() => ProxyFactory.Create(typeof(IList<object>), typeof(InterfaceInterceptor<IList<object>>), argTypes: new[] { typeof(IList<object>) }, args: new List<object>()));
            Assert.DoesNotThrow(() => ProxyFactory.Create(typeof(IList<object>), typeof(InterfaceInterceptor<IList<object>>), target: new List<object>()));
            Assert.DoesNotThrow(() => ProxyFactory.Create(typeof(IList<object>), typeof(InterfaceInterceptor<IList<object>>), args: new[] { new List<object>() }));

            var mockInjector = new Mock<IInjector>(MockBehavior.Strict);
            mockInjector
                .Setup(i => i.Get(typeof(IDisposable), null))
                .Returns(new Disposable());

            Assert.DoesNotThrow(() => ProxyFactory.Create(typeof(IList<object>), typeof(InterceptorHavingDependency), target: new List<object>(), injector: mockInjector.Object));

            mockInjector.Verify(i => i.Get(typeof(IDisposable), null), Times.Once);
        }

        [Test]
        public void Create_ShouldValidate() 
        {
            Assert.Throws<ArgumentException>(() => ProxyFactory.Create(typeof(object), typeof(InterfaceInterceptor<object>)));
            Assert.Throws<ArgumentException>(() => ProxyFactory.Create(typeof(IList<object>), typeof(InterfaceInterceptor<IList<int>>), null));
        }
    }
}
