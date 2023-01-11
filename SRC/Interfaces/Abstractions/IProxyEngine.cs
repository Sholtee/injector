/********************************************************************************
* IProxyEngine.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes how to make a proxy engine compatible with this library.
    /// </summary>
    public interface IProxyEngine
    {
        /// <summary>
        /// Creates a new proxy type against the given interfacce and target.
        /// </summary>
        /// <remarks>Target may be either the interface itself or a class implementing the interface.</remarks>
        Type CreateProxy(Type iface, Type target);

        /// <summary>
        /// Creates an expression that activates the proxy.
        /// </summary>
        /// <param name="proxy">The generated proxy type</param>
        /// <param name="injector">The injector to resolve dependencies</param>
        /// <param name="target">The target instance.</param>
        /// <param name="interceptorArray">An array containing all the related <see cref="IInterfaceInterceptor"/> instance.</param>
        /// <remarks>The returned expression should look like:
        /// <code>
        /// new MyProxy(target, interceptorArray)
        /// </code>
        /// </remarks>
        Expression CreateActivatorExpression(Type proxy, Expression injector, Expression target, Expression interceptorArray);
    }
}