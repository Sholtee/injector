/********************************************************************************
* InterfaceProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.DI.InterfaceProxy.Tests
{
    [TestFixture]
    public sealed class InterfaceProxyTests
    {
        private interface IMyInterface
        {
            int Hooked(int val);
            int NotHooked(int val);
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

        private sealed class MyProxy : InterfaceProxy<IMyInterface>
        {
            public MyProxy() : base(new MyClass())
            {
            }

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name == nameof(Target.Hooked)) return 1986;
                return base.Invoke(targetMethod, args);
            }
        }

        [Test]
        public void InterfaceProxy_ShouldHook()
        {
            IMyInterface myInterface = new MyProxy().Proxy;

            Assert.That(myInterface.NotHooked(1), Is.EqualTo(1));
            Assert.That(myInterface.Hooked(1), Is.EqualTo(1986));
        }
    }
}
