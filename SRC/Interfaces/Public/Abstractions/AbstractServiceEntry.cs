/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives;
    using Primitives.Threading;

    using static Properties.Resources;
    using static IServiceId.Formatter;

    /// <summary>
    /// Describes an abstract service entry.
    /// </summary>
    /// <remarks>Service entry specifies how to store, identify and instantiate a particular service.</remarks>
    public abstract class AbstractServiceEntry: IServiceId
    {
        private int FState;

        /// <summary>
        /// Value of <see cref="CreateInterceptorDelegate"/> property.
        /// </summary>
        protected FutureDelegate<CreateServiceDelegate>? FCreateInstance;

        /// <summary>
        /// Contains some constants regarding service resolution.
        /// </summary>
        public static class Consts
        {
            /// <summary>
            /// Invalid slot. Use this value when you want to create a new service instance on each request.
            /// </summary>
            public const int INVALID_SLOT = -1;
        }

        /// <summary>
        /// Provides the default implementation for the <see cref="UpdateState(ServiceEntryStates)"/> method.
        /// </summary>
        /// <returns>The original state that had been updated.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ServiceEntryStates UpdateStateInternal(ServiceEntryStates newState) =>
            //
            // As entries are shared between scopes, this method might be invoked parallelly
            // (by the GetOrCreateInstance() method) -> Interlocked.
            //

            (ServiceEntryStates) InterlockedExtensions.Or(ref FState, (int) newState);

        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="type">The type of the service.</param>
        /// <param name="key">The (optional) key of the service (usually a name).</param>
        /// <param name="implementation">The (optional) implementation of the service.</param>
        /// <param name="factory">The (optional) factory of the service.</param>
        /// <param name="explicitArgs">Optional explicit arguments (in form of {ctorArg1 = ..., ctorArg2 = ...}) to be passed to the constructor of <see cref="Implementation"/>.</param>
        /// <param name="options">Options to be assigned to this instance.</param>
        /// <exception cref="ArgumentException">The <paramref name="implementation"/> is not a class.</exception>
        protected AbstractServiceEntry(Type type, object? key, Type? implementation, Expression<FactoryDelegate>? factory, object? explicitArgs, ServiceOptions? options)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            //
            // The given "implementation" not necessarily implements the service type (for instance in case 
            // of Proxy recipe) therefore the missing validation.
            //

            if (implementation?.IsClass is false)
                throw new ArgumentException(NOT_A_CLASS, nameof(implementation));

            if (implementation?.IsAbstract is true)
                throw new ArgumentException(ABSTRACT_CLASS, nameof(implementation));

            Type = type;
            Key = key;
            Implementation = implementation;
            ExplicitArgs = explicitArgs;
            Factory = factory;
            Options = options;
        }

        #region Immutables
        /// <summary>
        /// The service type.
        /// </summary>
        /// <remarks>A service is identified by its <see cref="Type"/> and <see cref="Key"/>.</remarks>
        public Type Type { get; }

        /// <summary>
        /// The (optional) service key (usually a name).
        /// </summary>
        /// <remarks>A service is identified by its <see cref="Type"/> and <see cref="Key"/>.</remarks>
        public object? Key { get; }

        /// <summary>
        /// The (optional) implementation of the service.
        /// </summary>
        public Type? Implementation { get; }

        /// <summary>
        /// The (optional) factory of this service.
        /// </summary>
        public Expression<FactoryDelegate>? Factory { get; }

        /// <summary>
        /// The related lifetime.
        /// </summary>
        public virtual LifetimeBase? Lifetime { get; }

        /// <summary>
        /// Features related to this entry.
        /// </summary>
        public virtual ServiceEntryFeatures Features { get; }

        /// <summary>
        /// Optional explicit arguments to be passed to the constructor of <see cref="Implementation"/>. It is supposed to be either a dictionary or an anonymous object:
        /// <code>{ctorArg1 = ..., ctorArg2 = ...}</code>
        /// </summary>
        public object? ExplicitArgs { get; }

        /// <summary>
        /// Options assigned to this instance.
        /// </summary>
        public ServiceOptions? Options { get; }
        #endregion

        #region Mutables
        /// <summary>
        /// Current state of this entry.
        /// </summary>
        public ServiceEntryStates State
        {
            get => (ServiceEntryStates) FState;
            protected set => FState = (int) value;
        }

        /// <summary>
        /// The assigned slot (in case of scoped services).
        /// </summary>
        /// <remarks>To assign a proper value to this property, invoke the <see cref="Build(IBuildContext, IReadOnlyList{IFactoryVisitor})"/> method.</remarks>
        public int AssignedSlot { get; protected set; } = Consts.INVALID_SLOT;

        /// <summary>
        /// Unconditionaly creates a new service instance.
        /// </summary>
        /// <remarks>To assign value to this property, invoke the <see cref="Build(IBuildContext, IReadOnlyList{IFactoryVisitor})"/> method.</remarks>
        public CreateServiceDelegate? CreateInstance => FCreateInstance?.IsCompiled is true ? FCreateInstance.Value : null;

        /// <summary>
        /// Bound decorators.
        /// </summary>
        public virtual IReadOnlyList<Expression<DecoratorDelegate>> Decorators => throw new NotSupportedException(DECORATING_NOT_SUPPORTED);
        #endregion

        /// <summary>
        /// Specializes a service entry if it is generic.
        /// </summary>
        public virtual AbstractServiceEntry Specialize(params Type[] genericArguments) => throw new NotSupportedException(SPECIALIZATION_NOT_SUPPORTED);

        /// <summary>
        /// If implemented, alters the service instantiation process in order to wrap the original service into a proxy.
        /// </summary>
        public virtual void Decorate(Expression<DecoratorDelegate> decorator) => throw new NotSupportedException(DECORATING_NOT_SUPPORTED);

        /// <summary>
        /// Builds this entry applying the provided factory <paramref name="visitors"/>.
        /// </summary>
        public virtual void Build(IBuildContext context, IReadOnlyList<IFactoryVisitor> visitors) => throw new NotSupportedException();

        /// <summary>
        /// Creates the lifetime manager expression.
        /// </summary>
        /// <remarks>
        /// Lifetime manager instructs the system how to dispose a particular service instance. In most of cases it's just a simple cast:
        /// <code>disposable = getService() as IDisposable</code>
        /// </remarks>
        public virtual Expression CreateLifetimeManager(Expression getService, ParameterExpression scope, ParameterExpression disposable) => throw new NotSupportedException();

        /// <summary>
        /// If supported, updates the <see cref="State"/> of this entry.
        /// </summary>
        public virtual void UpdateState(ServiceEntryStates newState) => throw new NotSupportedException();

        /// <summary>
        /// Returns the short string representation of this entry.
        /// </summary>
        public override string ToString() => ToString(false); 

        /// <summary>
        /// Returns the string representation of this entry.
        /// </summary>
        public string ToString(bool shortForm)
        {
            StringBuilder result = new(Format(this));

            if (!shortForm)
            {
                const string
                    NAME_PART = " - {0}: {1}",
                    NULL = nameof(NULL);

                result
                    .AppendFormat(Culture, NAME_PART, nameof(Lifetime), Lifetime?.ToString() ?? NULL)
                    .AppendFormat(Culture, NAME_PART, nameof(Implementation), Implementation?.GetFriendlyName() ?? NULL)

                    #pragma warning disable CA1307 // Specify StringComparison for clarity
                    .AppendFormat(Culture, NAME_PART, nameof(Factory), Factory?.ToString().Replace(Environment.NewLine, " ") ?? NULL);
                    #pragma warning restore CA1307
            }

            return result.ToString();
        }
    }
}