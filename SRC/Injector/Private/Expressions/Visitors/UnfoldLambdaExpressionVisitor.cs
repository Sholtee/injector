/********************************************************************************
* UnfoldLambdaExpressionVisitor.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.ObjectModel;
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
        private readonly WriteOnce<ReadOnlyCollection<ParameterExpression>> FParameters = new();

        private readonly Expression[] FParameterSubstitutions;

        private UnfoldLambdaExpressionVisitor(Expression[] parameterSubstitutions) => FParameterSubstitutions = parameterSubstitutions;

        public static Expression Unfold(LambdaExpression lamda, params Expression[] parameterSubstitutions) =>
            new UnfoldLambdaExpressionVisitor(parameterSubstitutions).Visit(lamda);

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            if (FParameters.HasValue)
                //
                // In nested lambdas we just replace the captured compatible variables.
                //

                return base.VisitLambda(lambda);

            if (lambda.Parameters.Count != FParameterSubstitutions.Length)
                throw new NotSupportedException(Resources.LAMBDA_LAYOUT_NOT_SUPPORTED);

            FParameters.Value = lambda.Parameters;

            //
            // From the main method we just need the method body
            //

            return Visit(lambda.Body);
        }

        protected override Expression VisitParameter(ParameterExpression parameter)
        {
            //
            // Find the corresponding parameter
            //

            int index = FParameters.Value!.IndexOf(parameter);
            return index >= 0 ? FParameterSubstitutions[index] : parameter;
        }
    }
}
