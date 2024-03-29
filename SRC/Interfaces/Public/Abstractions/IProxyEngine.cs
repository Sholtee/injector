﻿/********************************************************************************
* IProxyEngine.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes how to wrap a proxy engine to be compatible with this library.
    /// </summary>
    /// <remarks>The default implementation can be found under the <c>Solti.Utils.DI</c> namespace which leverages the <a href="https://github.com/Sholtee/proxygen">ProxyGen.NET</a> library.</remarks>
    public interface IProxyEngine
    {
        /// <summary>
        /// Creates a new proxy type against the given interfacce and target.
        /// </summary>
        /// <remarks><paramref name="target"/> may be either the interface itself or a class implementing the interface.</remarks>
        Type CreateProxy(Type iface, Type target);

        /// <summary>
        /// Creates an expression that activates the proxy.
        /// </summary>
        /// <param name="proxy">The generated proxy type</param>
        /// <param name="injector">The injector to resolve dependencies</param>
        /// <param name="target">The target instance.</param>
        /// <param name="interceptorArray">An array containing all the related <see cref="IInterfaceInterceptor"/> instance.</param>
        /// <remarks>
        /// The returned expression should look like:
        /// <code>
        /// new MyProxy(target, interceptorArray)
        /// </code>
        /// </remarks>
        Expression CreateActivatorExpression(Type proxy, Expression injector, Expression target, Expression interceptorArray);
    }
}