/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives;
    using Properties;

    /// <summary>
    /// Describes an abstract service entry.
    /// </summary>
    public abstract class AbstractServiceEntry: IServiceId
    {
        /// <summary>
        /// Contains some constants related to the <see cref="IServiceActivator"/> interface.
        /// </summary>
        public static class Consts
        {
            /// <summary>
            /// Invalid slot..
            /// </summary>
            public const int INVALID_SLOT = -1;
        }

        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The interface of the service.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The (optional) implementation of the service.</param>
        /// <param name="factory">The (optional) factory of the service.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        /// <exception cref="ArgumentException">The <paramref name="implementation"/> is not a class.</exception>
        protected AbstractServiceEntry(Type @interface, string? name, Type? implementation, Expression<FactoryDelegate>? factory)
        {
            if (@interface is null)
                throw new ArgumentNullException(nameof(@interface));

            if (!@interface.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(@interface));

            //
            // The given "implementation" not necessarily implements the service interface (for instance in case 
            // of Proxy recipe) therefore the missing validation.
            //

            if (implementation?.IsClass is false)
                throw new ArgumentException(Resources.NOT_A_CLASS, nameof(implementation));

            Interface = @interface;
            Name = name;
            Implementation = implementation;
            Factory = factory;
        }

        #region Immutables
        /// <summary>
        /// The interface of the service.
        /// </summary>
        /// <remarks>A service is identified by its <see cref="Interface"/> and <see cref="Name"/>.</remarks>
        public Type Interface { get; }

        /// <summary>
        /// The (optional) name of the service.
        /// </summary>
        /// <remarks>A service is identified by its <see cref="Interface"/> and <see cref="Name"/>.</remarks>
        public string? Name { get; }

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
        #endregion

        #region Mutables
        /// <summary>
        /// Current state of this entry.
        /// </summary>
        public ServiceEntryStates State { get; protected set; }

        /// <summary>
        /// The assigned slot (in case of scoped services).
        /// </summary>
        /// <remarks>To assign a proper value to this property, invoke the <see cref="Build(IBuildContext, IReadOnlyList{IFactoryVisitor})"/> method.</remarks>
        public int AssignedSlot { get; protected set; } = Consts.INVALID_SLOT;

        /// <summary>
        /// Unconditionaly creates a new service instance.
        /// </summary>
        /// <remarks>To assign value to this property, invoke the <see cref="Build(IBuildContext, IReadOnlyList{IFactoryVisitor})"/> method.</remarks>
        public CreateServiceDelegate? CreateInstance { get; protected set; }

        /// <summary>
        /// Bound decorators.
        /// </summary>
        public virtual IReadOnlyList<Expression<DecoratorDelegate>> Decorators => throw new NotSupportedException(Resources.DECORATING_NOT_SUPPORTED);
        #endregion

        /// <summary>
        /// Specializes a service entry if it is generic.
        /// </summary>
        public virtual AbstractServiceEntry Specialize(params Type[] genericArguments) => throw new NotSupportedException(Resources.SPECIALIZATION_NOT_SUPPORTED);

        /// <summary>
        /// If implemented, alters the service instantiation process in order to wrap the original service into a proxy.
        /// </summary>
        public virtual void Decorate(Expression<DecoratorDelegate> decorator) => throw new NotSupportedException(Resources.DECORATING_NOT_SUPPORTED);

        /// <summary>
        /// Builds this entry applying the provided factory <paramref name="visitors"/>.
        /// </summary>
        public virtual void Build(IBuildContext context, IReadOnlyList<IFactoryVisitor> visitors) => throw new NotSupportedException();

        /// <summary>
        /// Creates the lifetime manager expression.
        /// </summary>
        public virtual Expression CreateLifetimeManager(Expression getService, ParameterExpression scope, ParameterExpression disposable) => throw new NotSupportedException();

        /// <summary>
        /// If supported, sets the <see cref="State"/> of this entry to <see cref="ServiceEntryStates.Validated"/>.
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
            StringBuilder result = new(Interface.GetFriendlyName());
            
            if (Name is not null)
                result.Append($":{Name}");

            if (!shortForm)
            {
                const string
                    NAME_PART = " - {0}: {1}",
                    NULL = nameof(NULL);

                result
                    .AppendFormat(Resources.Culture, NAME_PART, nameof(Lifetime), Lifetime?.ToString() ?? NULL)
                    .AppendFormat(Resources.Culture, NAME_PART, nameof(Implementation), Implementation?.GetFriendlyName() ?? NULL)

                    #pragma warning disable CA1307 // Specify StringComparison for clarity
                    .AppendFormat(Resources.Culture, NAME_PART, nameof(Factory), Factory?.ToString().Replace(Environment.NewLine, " ") ?? NULL);
                    #pragma warning restore CA1307
            }

            return result.ToString();
        }
    }
}