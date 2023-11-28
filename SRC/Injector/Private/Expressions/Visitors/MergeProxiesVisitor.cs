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
    /// (injector, type) => new Proxy_2
    /// (
    ///     target: new Proxy_1
    ///     (
    ///         target: factory(injector, type),
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
            if (entry.Decorators.Count > 0)
            {
                ParameterExpression
                    injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                    type = Expression.Parameter(typeof(Type), nameof(type));

                return Expression.Lambda<Func<IInjector, Type, object>>
                (
                    entry.Decorators.Aggregate
                    (
                        UnfoldLambdaExpressionVisitor.Unfold(factory, injector, type),
                        (inner, proxyExpr) => UnfoldLambdaExpressionVisitor.Unfold(proxyExpr, injector, type, inner)
                    ),
                    injector,
                    type
                );
            }

            return factory;
        }
    }
}
