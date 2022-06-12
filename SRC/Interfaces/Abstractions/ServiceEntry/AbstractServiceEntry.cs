/********************************************************************************
* AbstractServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Text;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives;
    using Properties;

    /// <summary>
    /// Describes an abstract service entry.
    /// </summary>
    public abstract class AbstractServiceEntry
    {
        /// <summary>
        /// Creates a new <see cref="AbstractServiceEntry"/> instance.
        /// </summary>
        /// <param name="interface">The interface of the service.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <param name="implementation">The (optional) implementation of the service.</param>
        /// <param name="factory">The (optional) factory of the service.</param>
        /// <exception cref="ArgumentException">The <paramref name="interface"/> is not an interface.</exception>
        /// <exception cref="ArgumentException">The <paramref name="implementation"/> does not support the <paramref name="interface"/>.</exception>
        protected AbstractServiceEntry(Type @interface, string? name, Type? implementation, Expression<Func<IInjector, Type, object>>? factory)
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
        public Expression<Func<IInjector, Type, object>>? Factory { get; }

        /// <summary>
        /// The related <see cref="Interfaces.Lifetime"/>.
        /// </summary>
        public virtual Lifetime? Lifetime { get; }

        /// <summary>
        /// Features related to this entry.
        /// </summary>
        public virtual ServiceEntryFlags Features { get; }
        #endregion

        #region Mutables
        /// <summary>
        /// Current state of this entry.
        /// </summary>
        public ServiceEntryStateFlags State { get; protected set; }
        #endregion

        /// <summary>
        /// Specializes a service entry if it is generic.
        /// </summary>
        public virtual AbstractServiceEntry Specialize(params Type[] genericArguments) => throw new NotSupportedException(Resources.SPECIALIZATION_NOT_SUPPORTED);

        /// <summary>
        /// If implemented, alters the service instantiation process in order to wrap the original service into a proxy.
        /// </summary>
        public virtual void ApplyProxy(Expression<Func<IInjector, Type, object, object>> applyProxy) => throw new NotSupportedException(Resources.PROXYING_NOT_SUPPORTED);

        /// <summary>
        /// Creates a new service instance. Should throw if the <see cref="State"/> is not <see cref="ServiceEntryStateFlags.Built"/>.
        /// </summary>
        public abstract object CreateInstance(IInjector scope, out object? lifetime);

        /// <summary>
        /// If supported, sets the <see cref="State"/> of this entry to <see cref="ServiceEntryStateFlags.Validated"/>.
        /// </summary>
        public virtual void SetValidated() => throw new NotSupportedException();

        /// <summary>
        /// Builds this entry.
        /// </summary>
        public virtual void Build(Func<LambdaExpression, LambdaExpression> visit) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override string ToString() => ToString(false); 

        /// <summary>
        /// Returns the string reprezentation of this entry.
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

        /// <summary>
        /// Returns the unique hash code of this entry. You are not allowed to override this method.
        /// </summary>
        public sealed override int GetHashCode() => base.GetHashCode();
    }
}