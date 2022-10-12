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

                Expression inner = UnfoldLambdaExpressionVisitor.Unfold(Factory, injector, iface);

                foreach (LambdaExpression applyProxyExpr in FProxies)
                {
                    inner = UnfoldLambdaExpressionVisitor.Unfold(applyProxyExpr, injector, iface, inner);
                }

                factoryExpr = Expression.Lambda<Func<IInjector, Type, object>>(inner, injector, iface);
            }
            else factoryExpr = Factory;

            factoryExpr = (Expression<Func<IInjector, Type, object>>) visitor(factoryExpr);

            if (compiler is not null)
            {
                Debug.WriteLine($"Created factory: {Environment.NewLine}{factoryExpr.GetDebugView()}");
                compiler.Compile(factoryExpr, factory => FBuiltFactory = factory);
            
                State = (State | ServiceEntryStates.Built) & ~ServiceEntryStates.Validated;
            }
        }
    }
}