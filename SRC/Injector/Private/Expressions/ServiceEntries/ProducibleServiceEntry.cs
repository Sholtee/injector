/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    public abstract partial class ProducibleServiceEntry
    {
        /// <inheritdoc/>
        public override Expression CreateLifetimeManager(Expression service, ParameterExpression scope, ParameterExpression disposable) =>
            //
            // disposable = service as IDisposable ?? (object?) (service as IAsyncDisposable);
            // return service;
            //

            Expression.Block
            (
                typeof(object),
                Expression.Assign
                (
                    disposable,
                    Expression.IfThenElse
                    (
                        Expression.Or
                        (
                            Expression.TypeIs(service, typeof(IDisposable)),
                            Expression.TypeIs(service, typeof(IAsyncDisposable))
                        ),
                        service,
                        Expression.Default(disposable.Type)
                    )
                ),
                service
            );
    }
}