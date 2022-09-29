/********************************************************************************
* GraphBuilderVisitor.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    internal sealed class GraphBuilderVisitor : ServiceRequestVisitor
    {
        private readonly DotGraphServiceEntryVisitor FBuilder;

        public GraphBuilderVisitor(DotGraphServiceEntryVisitor builder) => FBuilder = builder;

        protected override Expression VisitServiceRequest(MethodCallExpression method, Expression target, Type iface, string? name)
        {
            FBuilder.BuildById(iface, name);
            return method;
        }
    }
}
