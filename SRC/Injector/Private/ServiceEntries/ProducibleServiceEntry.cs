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

    /// <summary>
    /// Reperesents the base class of producible servce entries.
    /// </summary>
    public abstract partial class ProducibleServiceEntry : AbstractServiceEntry
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
        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type @interface!!, string? name) : base
        (
            @interface,
            name,
            null,
            null
        ) { }

        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type @interface!!, string? name, Expression<Func<IInjector, Type, object>> factory!!) : base
        (
            @interface,
            name,
            null,
            factory
        )
        {
            ApplyAspects();
        }

        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type @interface!!, string? name, Type implementation!!) : base
        (
            @interface,
            name,
            implementation,
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

        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type @interface!!, string? name, Type implementation!!, object explicitArgs!!) : base
        (
            @interface,
            name,
            implementation, 
            GetFactory
            (
                @interface,
                implementation,
                explicitArgs
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

        /// <inheritdoc/>
        public override void VisitFactory(Func<LambdaExpression, LambdaExpression> visitor!!, FactoryVisitorOptions options)
        {
            if (Factory is null)
                throw new InvalidOperationException(NOT_PRODUCIBLE);

            //
            // Chain all the related delegates
            //

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

            if (options.HasFlag(FactoryVisitorOptions.BuildDelegate))
            {
                Debug.WriteLine($"Created factory: {Environment.NewLine}{factory.GetDebugView()}");
                FBuiltFactory = factory.Compile();
            
                State = (State | ServiceEntryStates.Built) & ~ServiceEntryStates.Validated;
            }
        }

        /// <inheritdoc/>
        public override object CreateInstance(IInjector scope!!, out object? lifetime)
        {
            if (FBuiltFactory is null)
                throw new InvalidOperationException(NOT_BUILT);

            object result = FBuiltFactory(scope, Interface);

            lifetime = result as IDisposable ?? (object?) (result as IAsyncDisposable);
            return result;
        }

        /// <inheritdoc/>
        public override void SetValidated() => State |= ServiceEntryStates.Validated;

        /// <inheritdoc/>
        public override void ApplyProxy(Expression<Func<IInjector, Type, object, object>> applyProxy!!)
        {
            if (Factory is null)
                throw new NotSupportedException(PROXYING_NOT_SUPPORTED);

            FProxies ??= new List<Expression<Func<IInjector, Type, object, object>>>();
            FProxies.Add(applyProxy);
        }

        /// <summary>
        /// Explicit argument, applied on service instantiation.
        /// </summary>
        public object? ExplicitArgs { get; }

        /// <summary>
        /// The concrete factory. Not null when the entry is <see cref="ServiceEntryStates.Built"/>.
        /// </summary>
        public Func<IInjector, Type, object>? BuiltFactory => FBuiltFactory;

        /// <summary>
        /// The applied proxies.
        /// </summary>
        public IReadOnlyList<Expression<Func<IInjector, Type, object, object>>> Proxies =>
            (IReadOnlyList<Expression<Func<IInjector, Type, object, object>>>?) FProxies ?? Array<Expression<Func<IInjector, Type, object, object>>>.Empty;
    }
}