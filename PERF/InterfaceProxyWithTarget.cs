/********************************************************************************
* InterfaceProxyWithTarget.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Perf
{
    using Proxy;

    public class InterfaceProxyWithTarget : InterfaceInterceptor<IInterface>
    {
        public InterfaceProxyWithTarget(IInterface target) : base(target)
        {
        }        
    }
}