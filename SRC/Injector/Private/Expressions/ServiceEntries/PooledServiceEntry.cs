/********************************************************************************
* PooledServiceEntry.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed partial class PooledServiceEntry
    {  
        private static readonly MethodInfo FInvokePool = MethodInfoExtractor
            .Extract<object>(static res => InvokePool<object>(null!, null!, out res))
            .GetGenericMethodDefinition();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TInterface InvokePool<TInterface>(IInjector injector, string poolName, out object disposable) where TInterface : class
        {
            IPoolItem<TInterface> instance = injector
                .Get<IPool<TInterface>>(poolName)
                .Get();
            disposable = instance;
            return instance.Value;
        }

        public override Expression CreateLifetimeManager(Expression getService, ParameterExpression scope, ParameterExpression disposable) => Expression.Condition
        (
            test: UnfoldLambdaExpressionVisitor.Unfold
            (
                (Expression<Func<IInjector, bool>>) (static scope => POOL_SCOPE.Equals(scope.Tag)),
                scope
            ),

            //
            // In pool, we call the original factory
            //

            ifTrue: base.CreateLifetimeManager(getService, scope, disposable),

            //
            // On consumer side we get the actual service from the pool
            //

            ifFalse: Expression.Call
            (
                FInvokePool.MakeGenericMethod(Interface),
                scope,
                Expression.Constant(PoolName),
                disposable
            ),
            type: typeof(object)
        );
    }
}