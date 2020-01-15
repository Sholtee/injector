/********************************************************************************
* ProxyUtils.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.Proxy
{
    /// <summary>
    /// Defines some handy functions related to proxies.
    /// </summary>
    public static class ProxyUtils
    {
        /// <summary>
        /// Chains two or more proxies against the given <paramref name="seed"/>.
        /// </summary>
        /// <typeparam name="TInterface">The target interface.</typeparam>
        /// <param name="seed">The original instance on which the proxies will be applied.</param>
        /// <param name="proxies">Proxies to apply.</param>
        /// <returns>The proxied instance.</returns>
        public static TInterface Chain<TInterface>(TInterface seed, params Func<TInterface, TInterface>[] proxies) where TInterface: class =>
            proxies.Aggregate(seed, (current, factory) => factory(current));
    }
}
