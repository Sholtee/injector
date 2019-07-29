/********************************************************************************
* ProxyUtils.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.DI.Proxy
{
    /// <summary>
    /// Defines some handy utils related to proxies.
    /// </summary>
    public static class ProxyUtils
    {
        public static TInterface Chain<TInterface>(TInterface seed, params Func<TInterface, TInterface>[] proxies) where TInterface: class =>
            proxies.Aggregate(seed, (current, factory) => factory(current));
    }
}
