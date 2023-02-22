/********************************************************************************
* AspectAttributeEx.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;
    using Properties;

    /// <summary>
    /// Extends the <see cref="AspectAttribute"/> class in order to support "service recipe".
    /// </summary>
    public class AspectAttributeEx : AspectAttribute
    {
        /// <summary>
        /// Creates a new <see cref="AspectAttributeEx"/> instance.
        /// </summary>
        public AspectAttributeEx(Type interceptor, object? explicitArgs)
        {
            Interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
            ExplicitArgs = explicitArgs;
        }

        /// <summary>
        /// Creates a new <see cref="AspectAttributeEx"/> instance.
        /// </summary>
        public AspectAttributeEx(Type interceptor) : this(interceptor, null) { }

        /// <summary>
        /// Creates a new <see cref="AspectAttributeEx"/> instance.
        /// </summary>
        public AspectAttributeEx(Expression<CreateInterceptorDelegate> factory) : base(factory) { }

        /// <inheritdoc/>
        public override Expression<CreateInterceptorDelegate> GetFactory(AbstractServiceEntry relatedEntry)
        {
            if (Factory is not null)
                return Factory;

            if (relatedEntry is not ProducibleServiceEntry pse)
                throw new NotSupportedException(Resources.NOT_PRODUCIBLE);

            Debug.Assert(Interceptor is not null, "Interceptor must be specified");

            return DecoratorResolver.ResolveInterceptorFactory
            (
                Interceptor!,
                ExplicitArgs,
                pse.Options.DependencyResolvers
            );
        }

        /// <summary>
        /// The underlying interceptor.
        /// </summary>
        /// <remarks>This type must be a (instantiable) class implementing the <see cref="IInterfaceInterceptor"/> interface.</remarks>
        public Type? Interceptor { get; }

        /// <summary>
        /// Explicit arguments to be passed:
        /// <code>Explicitrgs = new {ctorParamName1 = ..., ctorParamName2 = ...}</code>
        /// </summary>
        public object? ExplicitArgs { get; }
    }
}
