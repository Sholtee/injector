/********************************************************************************
* PooledServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;

    internal sealed partial class PooledServiceEntry
    {
        private delegate object InvokePoolDelegate(IPool pool, out object disposable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object InvokePool(IPool pool, out object disposable)
        {
            object result = pool.Get();
            disposable = new PoolItemCheckin(pool, result);
            return result;
        }

        /*
        if (scope.Tag == POOL_SCOPE)
            //
            // In pool, we call the original factory
            //

            return base.CreateInstance(scope, out lifetime);
        else
        {
            //
            // On consumer side we get the item from the pool
            //

            IPool relatedPool = (IPool) scope.Get(PoolType, PoolName);

            object result = relatedPool.Get();
            lifetime = new PoolItemCheckin(relatedPool, result);
            return result;
        }
        */

        /// <inheritdoc/>
        public override Expression CreateLifetimeManager(Expression getService, ParameterExpression scope, ParameterExpression disposable) => Expression.Condition
        (
            test: UnfoldLambdaExpressionVisitor.Unfold
            (
                (Expression<Func<IInjector, bool>>) (static scope => POOL_SCOPE.Equals(scope.Tag)),
                scope
            ),
            ifTrue: base.CreateLifetimeManager(getService, scope, disposable),
            ifFalse: Expression.Invoke
            (
                Expression.Constant((InvokePoolDelegate) InvokePool),
                UnfoldLambdaExpressionVisitor.Unfold
                (
                    (Expression<Func<IInjector, IPool>>) (scope => (IPool) scope.Get(PoolType, PoolName)),
                    scope
                ),
                disposable
            ),
            type: typeof(object)
        );
    }
}