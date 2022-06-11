/********************************************************************************
* ServiceRequestReplacerDebug.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceRequestReplacerDebug : ServiceRequestReplacer
    {
        public ServiceRequestReplacerDebug(IServiceResolverLookup lookup, ServicePath path, bool permissive) : base(lookup, path, permissive)
        {
        }

        public int VisitedRequests { get; private set; }

        public int AlteredRequests { get; private set; }

        protected override Expression VisitServiceRequest(MethodCallExpression method, Expression target, Type iface, string? name)
        {
            VisitedRequests++;

            Expression result = base.VisitServiceRequest(method, target, iface, name);
            if (result != method)
                AlteredRequests++;

            return result;
        }
    }
}
