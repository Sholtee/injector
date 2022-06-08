/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    internal sealed partial class ServiceResolver : ServiceResolverBase
    {
        public override Expression GetResolveExpression(Expression instanceFactory) => Expression.Call
        (
            instanceFactory,
            FCreateInstance,
            Expression.Constant(FRelatedEntry)
        );
    }
}
