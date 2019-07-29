/********************************************************************************
* InterfaceProxyWithoutTarget.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.DI.Perf
{
    using Proxy;

    public class InterfaceProxyWithoutTarget : InterfaceInterceptor<IInterface>
    {
        public InterfaceProxyWithoutTarget() : base(null)
        {
        }

        public override object Invoke(MethodInfo targetMethod, object[] args) => 0;
    }
}