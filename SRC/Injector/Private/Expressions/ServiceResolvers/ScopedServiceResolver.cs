/********************************************************************************
* ScopedServiceResolver.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    internal sealed partial class ScopedServiceResolver : ScopedServiceResolverBase
    {
        public override Expression GetResolveExpression(Expression instanceFactory) => Expression.Call
        (
            instanceFactory,
            FGetOrCreateInstance,
            Expression.Constant(FRelatedEntry),
            Expression.Constant(FSlot)
        );
    }
}
