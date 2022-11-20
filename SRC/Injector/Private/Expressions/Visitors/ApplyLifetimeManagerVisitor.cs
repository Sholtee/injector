/********************************************************************************
* ApplyLifetimeManagerVisitor.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// Applies the lifetime manager, provided by the <see cref="AbstractServiceEntry"/>
    /// </summary>
    /// <remarks>
    /// <code>(_, _) => ...</code>
    /// becomes for instance
    /// <code>
    /// (_, _, out IDisposable? disposable) =>
    /// {
    ///     object svc = ...;
    ///     disposable = svc as IDisposable;
    ///     return svc;
    /// }
    /// </code>
    /// </remarks>
    internal sealed class ApplyLifetimeManagerVisitor : IFactoryVisitor
    {
        public LambdaExpression Visit(LambdaExpression factory, AbstractServiceEntry entry)
        {
            ParameterExpression
                scope = Expression.Parameter(typeof(IInstanceFactory), nameof(scope)),
                disposable = Expression.Parameter(typeof(object).MakeByRefType(), nameof(disposable));

            return Expression.Lambda<CreteServiceDelegate>
            (
                entry.CreateLifetimeManager
                (
                    UnfoldLambdaExpressionVisitor.Unfold(factory, scope, Expression.Constant(entry.Interface)),
                    scope,
                    disposable
                ),
                scope,
                disposable
            );
        }
    }
}
