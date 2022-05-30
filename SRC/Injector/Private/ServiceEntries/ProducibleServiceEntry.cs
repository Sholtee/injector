/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    using static Interfaces.Properties.Resources;
    using static Properties.Resources;

    internal abstract partial class ProducibleServiceEntry : AbstractServiceEntry
    {
        #region Private
        private List<Expression<Func<IInjector, Type, object, object>>>? FProxies;

        private Func<IInjector, Type, object>? FBuiltFactory;

        private static Expression<Func<IInjector, Type, object>>? GetFactory(Type @interface, Type implementation)
        {
            if (!@interface.IsGenericTypeDefinition)
                return ServiceActivator.Get(implementation);

            //
            // Just to validate the implementation.
            //

            implementation.GetApplicableConstructor();
            return null;
        }

        private static Expression<Func<IInjector, Type, object>>? GetFactory(Type @interface, Type implementation, object explicitArgs)
        {
            if (!@interface.IsGenericTypeDefinition)
                return explicitArgs is IReadOnlyDictionary<string, object?> dict
                    ? ServiceActivator.Get(implementation, dict)
                    : ServiceActivator.Get(implementation, explicitArgs);

            //
            // Just to validate the implementation.
            //

            implementation.GetApplicableConstructor();
            return null;
        }
        #endregion

        #region Protected
        protected ProducibleServiceEntry(Type @interface, string? name) : base
        (
            Ensure.Parameter.IsNotNull(@interface, nameof(@interface)),
            name,
            null,
            null
        ) { }

        protected ProducibleServiceEntry(Type @interface, string? name, Expression<Func<IInjector, Type, object>> factory) : base
        (
            Ensure.Parameter.IsNotNull(@interface, nameof(@interface)),
            name,
            null,
            Ensure.Parameter.IsNotNull(factory, nameof(factory))
        )
        {
            ApplyAspects();
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation) : base
        (
            Ensure.Parameter.IsNotNull(@interface, nameof(@interface)),
            name,
            Ensure.Parameter.IsNotNull(implementation, nameof(implementation)),
            GetFactory
            (
                @interface,
                implementation
            )
        )
        {
            if (Factory is not null)
                ApplyAspects();
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs) : base
        (
            Ensure.Parameter.IsNotNull(@interface, nameof(@interface)),
            name,
            Ensure.Parameter.IsNotNull(implementation, nameof(implementation)), 
            GetFactory
            (
                @interface,
                implementation,
                Ensure.Parameter.IsNotNull(explicitArgs, nameof(explicitArgs))
            )
        )
        {
            //
            // Ancestor does the rest of validation
            //

            ExplicitArgs = explicitArgs;

            if (Factory is not null)
                ApplyAspects();
        }
        #endregion

        public override void Build(Func<LambdaExpression, LambdaExpression> visitor)
        {
            if (visitor is null)
                throw new ArgumentNullException(nameof(visitor));

            if (Factory is null)
                throw new InvalidOperationException(NOT_PRODUCIBLE);

            //
            // Chain all the related delegates
            //
#if false
            Func<IInjector, Type, object> factory = (Func<IInjector, Type, object>) visitor(Factory).Compile();

            if (FProxies?.Count > 0)
            {
                foreach (Expression<Func<IInjector, Type, object, object>>? applyProxyExpr in FProxies)
                {
                    Func<IInjector, Type, object, object> applyProxy = (Func<IInjector, Type, object, object>) visitor(applyProxyExpr).Compile();

                    Func<IInjector, Type, object> innerFactory = factory;

                    factory = (injector, type) => applyProxy(injector, type, innerFactory(injector, type));
                }
            }

            FBuiltFactory = factory;
#else
            Expression<Func<IInjector, Type, object>> factory;

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

                factory = Expression.Lambda<Func<IInjector, Type, object>>(invocation, injector, iface);
            }
            else factory = (Expression<Func<IInjector, Type, object>>) visitor(Factory);

            Debug.WriteLine($"Created factory: {Environment.NewLine}{factory.GetDebugView()}");
            FBuiltFactory = factory.Compile();
#endif
            State = (State | ServiceEntryStateFlags.Built) & ~ServiceEntryStateFlags.Validated;
        }

        public override object CreateInstance(IInjector scope, out object? lifetime)
        {
            if (FBuiltFactory is null)
                throw new InvalidOperationException(NOT_BUILT);

            object result = FBuiltFactory(scope, Interface);

            //
            // Getting here indicates that the service graph validated successfully.
            //

            State |= ServiceEntryStateFlags.Validated;
            lifetime = result as IDisposable ?? (object?) (result as IAsyncDisposable);
            return result;
        }

        public override void ApplyProxy(Expression<Func<IInjector, Type, object, object>> applyProxy)
        {
            if (Factory is null)
                throw new NotSupportedException(PROXYING_NOT_SUPPORTED);

            FProxies ??= new List<Expression<Func<IInjector, Type, object, object>>>();
            FProxies.Add(applyProxy);
        }

        public object? ExplicitArgs { get; }

        public Func<IInjector, Type, object>? BuiltFactory => FBuiltFactory;

        public IReadOnlyList<Expression<Func<IInjector, Type, object, object>>> Proxies =>
            (IReadOnlyList<Expression<Func<IInjector, Type, object, object>>>?) FProxies ?? Array<Expression<Func<IInjector, Type, object, object>>>.Empty;
    }
}