/********************************************************************************
* GlobalServiceResolver.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    internal sealed partial class GlobalServiceResolver : ServiceResolverBase
    {
        public override Expression GetResolveExpression(Expression instanceFactory) => Expression.Call
        (
            Expression.Coalesce
            (
                Expression.Property(instanceFactory, FSuper),
                instanceFactory
            ),
            FCreateInstance,
            Expression.Constant(FRelatedEntry)
        );
    }
}
