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
    using Primitives.Patterns;

    internal sealed partial class PooledServiceEntry
    {  
        private static readonly MethodInfo FInvokePool = MethodInfoExtractor
            .Extract<object>(static res => InvokePool<object>(null!, null!, out res))
            .GetGenericMethodDefinition();

        private sealed class PoolItemCheckin<TInterface>: Disposable where TInterface : class
        {
            public PoolItemCheckin(IPool<TInterface> pool, TInterface instance)
            {
                Pool = pool;
                Instance = instance;
            }

            protected override void Dispose(bool disposeManaged)
            {
                base.Dispose(disposeManaged);
                Pool.Return(Instance);
            }

            public IPool<TInterface> Pool { get; }

            public TInterface Instance { get; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TInterface InvokePool<TInterface>(IInjector injector, string poolName, out object disposable) where TInterface : class
        {
            IPool<TInterface> pool = injector.Get<IPool<TInterface>>(poolName);
            TInterface instance = pool.Get();
            disposable = new PoolItemCheckin<TInterface>(pool, instance);
            return instance;
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