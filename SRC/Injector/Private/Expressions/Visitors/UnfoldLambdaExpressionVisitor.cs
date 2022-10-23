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
    using Primitives.Patterns;
    using Properties;

    /// <summary>
    /// Extracts the method body and replaces the parameters in the referencies.
    /// </summary>
    /// <remarks><code>param => param.DoComething();</code> becomes <code>substitution.DoSomething();</code></remarks>
    internal sealed class UnfoldLambdaExpressionVisitor : ExpressionVisitor
    {
        private readonly WriteOnce<IReadOnlyList<ParameterExpression>> FParameters = new();

        public IReadOnlyList<Expression> ParameterSubstitutions { get; }

        public UnfoldLambdaExpressionVisitor(IReadOnlyList<Expression> parameterSubstitutions) => ParameterSubstitutions = parameterSubstitutions;

        public static Expression Unfold(LambdaExpression lamda, params Expression[] parameterSubstitutions) =>
            new UnfoldLambdaExpressionVisitor(parameterSubstitutions).Visit(lamda);

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (FParameters.HasValue)
                //
                // In nested lambdas we just replace the captured compatible variables.
                //

                return base.VisitLambda(node);

            if (node.Parameters.Count != ParameterSubstitutions.Count)
                throw new NotSupportedException(Resources.LAMBDA_LAYOUT_NOT_SUPPORTED);

            FParameters!.Value = node.Parameters;

            //
            // From the main method we just need the method body
            //

            return Visit(node.Body);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            //
            // Find the corresponding parameter
            //

            int? index = FParameters
                .Value
                .Select((p, i) => new { Parameter = p, Index = i })
                .SingleOrDefault(p => p.Parameter == node)
                ?.Index;

            return index is not null
                ? ParameterSubstitutions[index.Value]
                : node;
        }
    }
}
