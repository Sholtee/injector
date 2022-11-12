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

    using static Interfaces.Properties.Resources;
    using static Properties.Resources;

    using Primitives;

    /// <summary>
    /// Reperesents the base class of producible servce entries.
    /// </summary>
    public abstract partial class ProducibleServiceEntry : AbstractServiceEntry
    {
        #region Private
        private readonly List<Expression<Func<IInjector, Type, object, object>>> FProxies = new();

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
        protected ProducibleServiceEntry(Type @interface, string? name) : base
        (
            @interface ?? throw new ArgumentNullException(nameof(@interface)),
            name,
            null,
            null
        ) { }

        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type @interface, string? name, Expression<Func<IInjector, Type, object>> factory) : base
        (
            @interface ?? throw new ArgumentNullException(nameof(@interface)),
            name,
            null,
            factory ?? throw new ArgumentNullException(nameof(factory))
        ) => ApplyAspects();

        /// <summary>
        /// Creartes a new <see cref="ProducibleServiceEntry"/> instance.
        /// </summary>
        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation) : base
        (
            @interface ?? throw new ArgumentNullException(nameof(@interface)),
            name,
            implementation ?? throw new ArgumentNullException(nameof(implementation)),
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
        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, object explicitArgs) : base
        (
            @interface ?? throw new ArgumentNullException(nameof(@interface)),
            name,
            implementation ?? throw new ArgumentNullException(nameof(implementation)), 
            GetFactory
            (
                @interface,
                implementation,
                explicitArgs ?? throw new ArgumentNullException(nameof(explicitArgs))
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
        public override void Build(IDelegateCompiler? compiler, ref int slots, params IFactoryVisitor[] visitors)
        {
            if (visitors is null)
                throw new ArgumentNullException(nameof(visitors));

            if (Factory is null)
                throw new InvalidOperationException(NOT_PRODUCIBLE);

            //
            // Chain all the related delegates
            //

            LambdaExpression factoryExpr = visitors.Aggregate<IFactoryVisitor, LambdaExpression>
            (
                Factory,
                (visited, visitor) => visitor.Visit(visited, this)
            );

            if (compiler is not null)
            {
                Debug.WriteLine($"Created factory: {Environment.NewLine}{factoryExpr.GetDebugView()}");
                compiler.Compile((Expression<FactoryDelegate>) factoryExpr, factory => CreateInstance = factory);

                State = (State | ServiceEntryStates.Built) & ~ServiceEntryStates.Validated;
            }
        }

        /// <inheritdoc/>
        public sealed override void SetValidated() => State |= ServiceEntryStates.Validated;

        /// <inheritdoc/>
        public sealed override void ApplyProxy(Expression<Func<IInjector, Type, object, object>> applyProxy)
        {
            if (applyProxy is null)
                throw new ArgumentNullException(nameof(applyProxy));

            if (Factory is null)
                throw new NotSupportedException(PROXYING_NOT_SUPPORTED);

            FProxies.Add(applyProxy);
        }

        /// <summary>
        /// Explicit argument, applied on service instantiation.
        /// </summary>
        public object? ExplicitArgs { get; }

        /// <summary>
        /// The applied proxies.
        /// </summary>
        public sealed override IReadOnlyList<Expression<Func<IInjector, Type, object, object>>> Proxies => FProxies;
    }
}