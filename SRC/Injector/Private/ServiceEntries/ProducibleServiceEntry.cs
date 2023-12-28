/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

        private static Expression<FactoryDelegate>? GetFactory(Type type, Type implementation, object? explicitArgs, ServiceOptions? options)
        {
            if (!implementation.IsClass)
                throw new ArgumentException(NOT_A_CLASS, nameof(implementation));

            ConstructorInfo ctor = implementation.GetApplicableConstructor();  // validates the implementation even in case of a generic svc
            return !type.IsGenericTypeDefinition
                ? new ServiceActivator(options).ResolveFactory(ctor, explicitArgs)
                : null;
        }

        private ProducibleServiceEntry(Type type, object? key, Type? implementation, Expression<FactoryDelegate>? factory, object? explicitArgs, ServiceOptions? options) : base(type, key, implementation, factory, explicitArgs, options ?? ServiceOptions.Default)
        {
            if (Options!.SupportAspects)
            {
                //
                // Since aspects may target the implementation itself, they must be applied first
                //

                Debug.Assert(Decorators.Count is 0, "Aspects must be applied first");
                Features = ServiceEntryFeatures.SupportsAspects;
                if (Factory is not null)
                    this.ApplyAspects();
            }
        }
        #endregion

        #region Protected
        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type type, object? key, Expression<FactoryDelegate> factory, ServiceOptions? options) : this
        (
            type ?? throw new ArgumentNullException(nameof(type)),
            key,
            null,
            factory ?? throw new ArgumentNullException(nameof(factory)),
            null,
            options
        ) {}

        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type type, object? key, Type implementation, ServiceOptions? options) : this
        (
            type ?? throw new ArgumentNullException(nameof(type)),
            key,
            implementation ?? throw new ArgumentNullException(nameof(implementation)),
            GetFactory
            (
                type,
                implementation,
                null,
                options
            ),
            null,
            options
        ) {}

        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type type, object? key, Type implementation, object explicitArgs, ServiceOptions? options) : this
        (
            type ?? throw new ArgumentNullException(nameof(type)),
            key,
            implementation ?? throw new ArgumentNullException(nameof(implementation)),
            GetFactory
            (
                type,
                implementation,
                explicitArgs ?? throw new ArgumentNullException(nameof(explicitArgs)),
                options
            ),
            explicitArgs,
            options
        ) {}
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
            FCreateInstance = context
                .Compiler
                .Register(factoryExpr);

            State = (State | ServiceEntryStates.Built) & ~ServiceEntryStates.Validated;
        }

        /// <inheritdoc/>
        public sealed override void UpdateState(ServiceEntryStates newState)
        {
            Debug.WriteLineIf(newState < State, $"Downgrading state of {this}");
            UpdateStateInternal(newState);
        }

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
        /// Bound decorators.
        /// </summary>
        public sealed override IReadOnlyList<Expression<DecoratorDelegate>> Decorators => FDecorators;
    }
}