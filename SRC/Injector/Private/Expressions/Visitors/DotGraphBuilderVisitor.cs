/********************************************************************************
* DotGraphBuilderVisitor.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// Visits the dependency tree to build <a href="https://graphviz.org/">DOT graph</a> graph.
    /// </summary>
    internal sealed class DotGraphBuilderVisitor : ServiceRequestVisitor, IFactoryVisitor
    {
        private readonly DotGraphBuilder FBuilder;

        public DotGraphBuilderVisitor(DotGraphBuilder builder) => FBuilder = builder;

        public LambdaExpression Visit(LambdaExpression factory, AbstractServiceEntry entry) => (LambdaExpression) Visit(factory);

        protected override Expression VisitServiceRequest(MethodCallExpression method, Expression target, Type iface, string? name)
        {
            FBuilder.Build(iface, name);
            return method;
        }
    }
}
