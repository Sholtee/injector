/********************************************************************************
* ProxyFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using Moq;
using NUnit.Framework;

[assembly: InternalsVisibleTo("Solti.Utils.Proxy.InterfaceInterceptor<System.Collections.Generic.IList<Solti.Utils.Proxy.Tests.ProxyFactoryTests.MyEntity>>_System.Collections.Generic.IList<Solti.Utils.Proxy.Tests.ProxyFactoryTests.MyEntity>_Proxy")]

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
            mockInjector
                .Setup(i => i.Instantiate(It.Is<Type>(t => typeof(InterfaceInterceptor<IList<object>>).IsAssignableFrom(t)), It.Is<Dictionary<string, object>>(d => d.ContainsKey("target"))))
                .Returns<Type, Dictionary<string, object>>((t, args) => Resolver.GetExtended(t).Invoke(mockInjector.Object, args));

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
            mockInjector
                .Setup(i => i.Instantiate(It.Is<Type>(t => typeof(InterfaceInterceptor<IList<object>>).IsAssignableFrom(t)), It.Is<Dictionary<string, object>>(d => d.ContainsKey("target"))))
                .Returns<Type, Dictionary<string, object>>((t, args) => Resolver.GetExtended(t).Invoke(mockInjector.Object, args));

            Assert.DoesNotThrow(() => ProxyFactory.Create(typeof(IList<object>), typeof(InterceptorHavingDependency), target: new List<object>(), injector: mockInjector.Object));

            mockInjector.Verify(i => i.Get(typeof(IDisposable), null), Times.Once);
        }

        [Test]
        public void Create_ShouldValidate() 
        {
            Assert.Throws<InvalidOperationException>(() => ProxyFactory.Create(typeof(object), typeof(InterfaceInterceptor<object>)));
            Assert.Throws<ArgumentException>(() => ProxyFactory.Create(typeof(IList<object>), typeof(InterfaceInterceptor<IList<int>>), null));
        }

        internal class MyEntity { } // csak a lenti tesztben van hasznalva -> teszt nem akadhat ossze mas koddal

        [Test]
        public void Create_ShouldCacheTheGeneratedAssembly() 
        {
            string cacheDir = TypeGeneratorExtensions.GetCacheDirectory<IList<MyEntity>, ProxyGenerator<IList<MyEntity>, InterfaceInterceptor<IList<MyEntity>>>>();
            Assert.That(!Directory.Exists(cacheDir));

            bool oldVal = ProxyFactory.PreserveProxyAssemblies;
            ProxyFactory.PreserveProxyAssemblies = true;

            try
            {
                Assert.DoesNotThrow(() => ProxyFactory.Create(typeof(IList<MyEntity>), typeof(InterfaceInterceptor<IList<MyEntity>>), target: new List<MyEntity>()));
                Assert.That(Directory.EnumerateFiles(cacheDir).Any());
            }
            finally 
            {
                ProxyFactory.PreserveProxyAssemblies = oldVal;

                if (Directory.Exists(cacheDir))
                    Directory.Delete(cacheDir, true);
            }
        }
    }
}
