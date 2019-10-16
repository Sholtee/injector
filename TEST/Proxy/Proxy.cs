/********************************************************************************
* InterfaceProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.DI.Proxy.Tests
{
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
            IMyInterface myInterface = ProxyFactory.Create<IMyInterface, MyProxy>(new MyClass());

            Assert.That(myInterface.NotHooked(1), Is.EqualTo(1));
            Assert.That(myInterface.Hooked(1), Is.EqualTo(1986));
        }

        [Test]
        public void GetGeneratedProxyType_ShouldCache() 
        {
            Assert.AreSame(ProxyFactory.GetGeneratedProxyType(typeof(IMyInterface), typeof(MyProxy)), ProxyFactory.GetGeneratedProxyType(typeof(IMyInterface), typeof(MyProxy)));
        }
    }
}
