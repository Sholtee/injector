/********************************************************************************
* MergeProxiesVisitor.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// Merges proxy lambads to a single expression.
    /// </summary>
    /// <remarks>
    /// <code>
    /// (injector, iface) => new Proxy_2
    /// (
    ///     target: new Proxy_1
    ///     (
    ///         target: factory(injector, iface),
    ///         ...
    ///     ),
    ///     ...
    /// )
    /// </code>
    /// </remarks>
    internal sealed class MergeProxiesVisitor : IFactoryVisitor
    {
        public LambdaExpression Visit(LambdaExpression factory, AbstractServiceEntry entry)
        {
            if (entry.Proxies.Count > 0)
            {
                ParameterExpression
                    injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                    iface = Expression.Parameter(typeof(Type), nameof(iface));

                return Expression.Lambda<Func<IInjector, Type, object>>
                (
                    entry.Proxies.Aggregate
                    (
                        UnfoldLambdaExpressionVisitor.Unfold(factory, injector, iface),
                        (inner, proxyExpr) => UnfoldLambdaExpressionVisitor.Unfold(proxyExpr, injector, iface, inner)
                    ),
                    injector,
                    iface
                );
            }

            return factory;
        }
    }
}
