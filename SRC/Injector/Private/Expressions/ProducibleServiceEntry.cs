/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    using static Properties.Resources;

    public abstract partial class ProducibleServiceEntry
    {
        /// <inheritdoc/>
        public override void VisitFactory(Func<LambdaExpression, LambdaExpression> visitor, IDelegateCompiler? compiler)
        {
            if (visitor is null)
                throw new ArgumentNullException(nameof(visitor));

            if (Factory is null)
                throw new InvalidOperationException(NOT_PRODUCIBLE);

            //
            // Chain all the related delegates
            //

            Expression<Func<IInjector, Type, object>> factoryExpr;

            if (FProxies?.Count > 0)
            {
                ParameterExpression
                    injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                    iface = Expression.Parameter(typeof(Type), nameof(iface));

                InvocationExpression invocation = Expression.Invoke
                (
                    visitor(Factory),
                    injector,
                    iface
                );

                foreach (Expression<Func<IInjector, Type, object, object>>? applyProxyExpr in FProxies)
                {
                    invocation = Expression.Invoke
                    (
                        visitor(applyProxyExpr),
                        injector,
                        iface,
                        invocation
                    );
                }

                factoryExpr = Expression.Lambda<Func<IInjector, Type, object>>(invocation, injector, iface);
            }
            else factoryExpr = (Expression<Func<IInjector, Type, object>>) visitor(Factory);

            if (compiler is not null)
            {
                Debug.WriteLine($"Created factory: {Environment.NewLine}{factoryExpr.GetDebugView()}");
                compiler.Compile(factoryExpr, factory => FBuiltFactory = factory);
            
                State = (State | ServiceEntryStates.Built) & ~ServiceEntryStates.Validated;
            }
        }
    }
}