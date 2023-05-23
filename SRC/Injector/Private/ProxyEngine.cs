/********************************************************************************
* ProxyEngine.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Proxy.Generators;

    internal sealed partial class ProxyEngine : Singleton<ProxyEngine>, IProxyEngine
    {
        public Type CreateProxy(Type iface, Type target) => new ProxyGenerator
        (
            iface,
            typeof(InterceptorAggregator<,>).MakeGenericType(iface, target)
        ).GetGeneratedType();
    }
}
