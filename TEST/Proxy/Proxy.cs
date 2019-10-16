/********************************************************************************
* InterfaceProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.DI.Proxy.Tests
{
    using Internals;
    using Proxy;

    [TestFixture]
    public sealed class ProxyTests
    {
        public interface IMyInterface
        {
            int Hooked(int val);
            int NotHooked(int val);
        }

        public class MyProxy : InterfaceInterceptor<IMyInterface>
        {
            public override object Invoke(MethodInfo targetMethod, object[] args, MemberInfo extra)
            {
                if (targetMethod.Name == nameof(Target.Hooked)) return 1986;
                return base.Invoke(targetMethod, args, extra);
            }

            public MyProxy(IMyInterface target) : base(target)
            {
            }        
        }

        public class MyProxyEx : MyProxy 
        {
            public MyProxyEx(IDisposable dependency, IMyInterface target) : base(target)
            {
                Dependency = dependency;
            }

            public IDisposable Dependency { get; }
        }

        private sealed class MyClass : IMyInterface
        {
            public int Hooked(int val)
            {
                return val;
            }

            public int NotHooked(int val)
            {
                return val;
            }
        }

        [Test]
        public void InterfaceProxy_ShouldHook()
        {
            foreach (IMyInterface myInterface in new[]
            {
                ProxyFactory.Create<IMyInterface, MyProxy>(new MyClass()),
                ProxyFactory.Create(typeof(IMyInterface), typeof(MyProxy), new MyClass()) as IMyInterface
            })
            {
                Assert.NotNull(myInterface);
                Assert.That(myInterface.NotHooked(1), Is.EqualTo(1));
                Assert.That(myInterface.Hooked(1), Is.EqualTo(1986));
            }
        }

        [Test]
        public void InterfaceProxy_MayUseInjector() 
        {
            using (IServiceContainer container = new ServiceContainer()) 
            {
                IDisposable instance = new Disposable();

                IInjector injector = container
                    .Instance(instance)
                    .CreateInjector();

                foreach (IMyInterface myInterface in new[] 
                { 
                    ProxyFactory.Create(typeof(IMyInterface), typeof(MyProxyEx), new MyClass(), injector) as IMyInterface, 
                    ProxyFactory.Create<IMyInterface, MyProxyEx>(new MyClass(), injector) 
                })
                {
                    Assert.NotNull(myInterface);
                    Assert.That(myInterface.NotHooked(1), Is.EqualTo(1));
                    Assert.That(myInterface.Hooked(1), Is.EqualTo(1986));
                    Assert.That(myInterface is MyProxyEx);
                    Assert.AreSame(((MyProxyEx) myInterface).Dependency, instance);
                }
            }
        }

        [Test]
        public void GetGeneratedProxyType_ShouldCache() 
        {
            Assert.AreSame(ProxyFactory.GetGeneratedProxyType(typeof(IMyInterface), typeof(MyProxy)), ProxyFactory.GetGeneratedProxyType(typeof(IMyInterface), typeof(MyProxy)));
        }

        [Test]
        public void GetGeneratedProxyType_ShouldValidate() 
        {
            var ex = Assert.Throws<ArgumentException>(() => ProxyFactory.GetGeneratedProxyType(typeof(IMyInterface), typeof(InterfaceInterceptor<IDisposable>)));
            Assert.That(ex.ParamName, Is.EqualTo("interceptor"));

            ex = Assert.Throws<ArgumentException>(() => ProxyFactory.GetGeneratedProxyType(typeof(object), typeof(InterfaceInterceptor<object>)));
            Assert.That(ex.ParamName, Is.EqualTo("iface"));
        }
    }
}
