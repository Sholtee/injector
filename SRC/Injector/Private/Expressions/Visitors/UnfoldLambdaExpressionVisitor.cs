/********************************************************************************
* UnfoldLambdaExpressionVisitor.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class UnfoldLambdaExpressionVisitor : ExpressionVisitor
    {
        private IReadOnlyList<ParameterExpression>? FParameters;

        public IReadOnlyList<Expression> ParameterSubstitutions { get; }

        public UnfoldLambdaExpressionVisitor(IReadOnlyList<Expression> parameterSubstitutions) => ParameterSubstitutions = parameterSubstitutions;

        public static Expression Unfold(LambdaExpression lamda, params Expression[] parameterSubstitutions) =>
            new UnfoldLambdaExpressionVisitor(parameterSubstitutions).Visit(lamda);

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (FParameters is not null)
                //
                // We have no business with nested lambdas.
                //

                return node;

            if (node.Parameters.Count != ParameterSubstitutions.Count)
                throw new NotSupportedException(Resources.LAMBDA_LAYOUT_NOT_SUPPORTED);

            FParameters = node.Parameters;

            //
            // We just need the method body
            //

            return Visit(node.Body);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            //
            // Don't deal with variables
            //

            int? index = FParameters
                ?.Select((p, i) => new { Parameter = p, Index = i })
                .SingleOrDefault(p => p.Parameter == node)
                ?.Index;

            return index is not null
                ? ParameterSubstitutions[index.Value]
                : node;
        }
    }
}
