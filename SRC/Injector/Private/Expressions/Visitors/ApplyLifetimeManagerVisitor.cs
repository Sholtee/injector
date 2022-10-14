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
                fact = Expression.Parameter(typeof(IInstanceFactory), nameof(fact)),
                disposable = Expression.Parameter(typeof(object), nameof(disposable));

            return Expression.Lambda<FactoryDelegate>
            (
                entry.CreateLifetimeManager
                (
                    UnfoldLambdaExpressionVisitor.Unfold(factory, fact, Expression.Constant(entry.Interface)),
                    fact,
                    disposable
                ),
                fact,
                disposable
            );
        }
    }
}
