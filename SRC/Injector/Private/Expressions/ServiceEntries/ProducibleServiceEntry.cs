/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    public abstract partial class ProducibleServiceEntry
    {
        /// <inheritdoc/>
        public override Expression CreateLifetimeManager(Expression getService, ParameterExpression scope, ParameterExpression disposable)
        {
            if (Options.DisposalMode is ServiceDisposalMode.Suppress)
                return getService;

            if (Options.DisposalMode is ServiceDisposalMode.Default && !typeof(IDisposable).IsAssignableFrom(Interface) && ! typeof(IAsyncDisposable).IsAssignableFrom(Interface))
                return getService;

            //
            // disposable = service as IDisposable ?? (object?) (service as IAsyncDisposable);
            // return service;
            //

            ParameterExpression service = Expression.Parameter(getService.Type, nameof(service));

            return Expression.Block
            (
                type: typeof(object),
                variables: new[] { service },
                Expression.Assign(service, getService),
                Expression.Assign
                (
                    disposable,
                    Expression.Condition
                    (
                        Expression.Or
                        (
                            Expression.TypeIs(service, typeof(IDisposable)),
                            Expression.TypeIs(service, typeof(IAsyncDisposable))
                        ),
                        service,
                        Expression.Default(disposable.Type),
                        disposable.Type
                    )
                ),
                service
            );
        }
    }
}