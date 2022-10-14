/********************************************************************************
* ApplyLifetimeManagerVisitor.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ApplyLifetimeManagerVisitor : IFactoryVisitor
    {
        public LambdaExpression Visit(LambdaExpression factory, AbstractServiceEntry entry)
        {
            ParameterExpression
                scope = Expression.Parameter(typeof(IInstanceFactory), nameof(scope)),
                disposable = Expression.Parameter(typeof(object).MakeByRefType(), nameof(disposable));

            return Expression.Lambda<FactoryDelegate>
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
