﻿/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly List<Expression<DecoratorDelegate>> FDecorators = new();

        private static Expression<FactoryDelegate>? GetFactory(Type @interface, Type implementation)
        {
            if (!@interface.IsGenericTypeDefinition)
                return ServiceActivator.Get(implementation);

            //
            // Just to validate the implementation.
            //

            implementation.GetApplicableConstructor();
            return null;
        }

        private static Expression<FactoryDelegate>? GetFactory(Type @interface, Type implementation, object explicitArgs)
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

        private ProducibleServiceEntry(Type @interface, string? name, Type? implementation, Expression<FactoryDelegate>? factory, ServiceOptions options) : base(@interface, name, implementation, factory)
        {
            Options = options;
            if (Options.SupportAspects)
            {
                Debug.Assert(Decorators.Count is 0, "Aspects must be applied first");
                Features = ServiceEntryFeatures.SupportsAspects;
                if (Factory is not null)
                {
                    Expression<DecoratorDelegate>? decorator = ServiceActivator.GetDecoratorForAspects
                    (
                        Interface,
                        Implementation ?? Interface,
                        Options.ProxyEngine ?? ProxyEngine.Instance
                    );
                    if (decorator is not null)
                        Decorate(decorator);
                }
            }
        }
        #endregion

        #region Protected
        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type @interface, string? name, Expression<FactoryDelegate> factory, ServiceOptions options) : this
        (
            @interface ?? throw new ArgumentNullException(nameof(@interface)),
            name,
            null,
            factory ?? throw new ArgumentNullException(nameof(factory)),
            options ?? throw new ArgumentNullException(nameof(options))
        ) {}

        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, ServiceOptions options) : this
        (
            @interface ?? throw new ArgumentNullException(nameof(@interface)),
            name,
            implementation ?? throw new ArgumentNullException(nameof(implementation)),
            GetFactory
            (
                @interface,
                implementation
            ),
            options ?? throw new ArgumentNullException(nameof(options))
        ) {}

        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs, ServiceOptions options) : this
        (
            @interface ?? throw new ArgumentNullException(nameof(@interface)),
            name,
            implementation ?? throw new ArgumentNullException(nameof(implementation)), 
            GetFactory
            (
                @interface,
                implementation,
                explicitArgs ?? throw new ArgumentNullException(nameof(explicitArgs))
            ),
            options ?? throw new ArgumentNullException(nameof(options))
        )
            //
            // Ancestor does the rest of validation
            //

            => ExplicitArgs = explicitArgs;
        #endregion

        /// <inheritdoc/>
        public override void Build(IBuildContext context, IReadOnlyList<IFactoryVisitor> visitors)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (visitors is null)
                throw new ArgumentNullException(nameof(visitors));

            if (Factory is null)
                throw new InvalidOperationException(NOT_PRODUCIBLE);

            //
            // Chain all the related delegates
            //

            Expression<CreateServiceDelegate> factoryExpr = (Expression<CreateServiceDelegate>) visitors.Aggregate<IFactoryVisitor, LambdaExpression>
            (
                Factory,
                (visited, visitor) => visitor.Visit(visited, this)
            );

            Debug.WriteLine($"Created factory: {Environment.NewLine}{factoryExpr.GetDebugView()}");
            context
                .Compiler
                .Compile(factoryExpr, factory => CreateInstance = factory);

            State = (State | ServiceEntryStates.Built) & ~ServiceEntryStates.Validated;
        }

        /// <inheritdoc/>
        public sealed override void SetValidated() => State |= ServiceEntryStates.Validated;

        /// <inheritdoc/>
        public override ServiceEntryFeatures Features { get; }

        /// <inheritdoc/>
        public override void Decorate(Expression<DecoratorDelegate> decorator)
        {
            if (Factory is null)
                throw new NotSupportedException(DECORATING_NOT_SUPPORTED);

            FDecorators.Add(decorator ?? throw new ArgumentNullException(nameof(decorator)));
        }

        /// <summary>
        /// Explicit argument, applied on service instantiation.
        /// </summary>
        public object? ExplicitArgs { get; }

        /// <summary>
        /// Options assigned to this instance.
        /// </summary>
        public ServiceOptions Options { get; }

        /// <summary>
        /// Bound decorators.
        /// </summary>
        public sealed override IReadOnlyList<Expression<DecoratorDelegate>> Decorators => FDecorators;
    }
}