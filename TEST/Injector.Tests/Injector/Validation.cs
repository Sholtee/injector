/********************************************************************************
* Validation.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.DI.Injector.Validation.Tests
{
    using Diagnostics;
    using Proxy;

    [TestFixture]
    public class ValidationTests
    {
        [Test]
        public void GetUnderlyingInstance_ShouldReturnTheActualObjectItIsNotAPRoxy()
        {
            IList<int> obj = new List<int>();
            Assert.AreSame(obj, IInjectorExtensions.GetUnderlyingInstance(obj, typeof(IList<int>)));
        }

        [Test]
        public void GetUnderlyingInstance_ShouldReturnTheUnderlyingObjectTheTheActualObjectIsAProxy()
        {

            IList<int>
                obj = new List<int>(),
                proxy = ProxyFactory.Create<IList<int>, InterfaceInterceptor<IList<int>>>(obj);

            Assert.AreSame(obj, IInjectorExtensions.GetUnderlyingInstance(proxy, typeof(IList<int>)));
        }
    }
}
