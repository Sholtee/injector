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
            switch (Options.DisposalMode)
            {
                case ServiceDisposalMode.Soft:
                    if (typeof(IDisposable).IsAssignableFrom(Interface) || typeof(IAsyncDisposable).IsAssignableFrom(Interface))
                        goto case ServiceDisposalMode.Force;
                    return getService;
                case ServiceDisposalMode.Force:
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
                case ServiceDisposalMode.Suppress:
                    return getService;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}