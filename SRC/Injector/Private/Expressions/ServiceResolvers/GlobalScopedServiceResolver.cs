/********************************************************************************
* GlobalScopedServiceResolver.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    internal sealed partial class GlobalScopedServiceResolver : ScopedServiceResolverBase
    {
        public override Expression GetResolveExpression(Expression instanceFactory) => Expression.Call
        (
            Expression.Coalesce
            (
                Expression.Property(instanceFactory, FSuper),
                instanceFactory
            ),
            FGetOrCreateInstance,
            Expression.Constant(FRelatedEntry),
            Expression.Constant(FSlot)
        );
    }
}
