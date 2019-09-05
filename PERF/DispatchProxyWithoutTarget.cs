/********************************************************************************
* DispatchProxyWithoutTarget.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.DI.Perf
{
    public class DispatchProxyWithoutTarget : DispatchProxy
    {
        protected override object Invoke(MethodInfo targetMethod, object[] args) => 0;
    }
}