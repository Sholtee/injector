/********************************************************************************
* DuckFactory.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.Proxy
{
    using Generators;

    using DI.Internals;

    /// <summary>
    /// Generates duck-typing proxy objects.
    /// </summary>
    public abstract class DuckFactory 
    {
        /// <summary>
        /// Generates a proxy object to let the target behave like a <typeparamref name="TInterface"/> instance.
        /// </summary>
        /// <remarks>The target must declare all the <typeparamref name="TInterface"/> members publicly.</remarks>
        /// <returns>The newly created proxy object.</returns>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        public abstract TInterface Like<TInterface>() where TInterface : class;

        /// <summary>
        /// Creates a new typed <see cref="DuckFactory"/> instance.
        /// </summary>
        /// <typeparam name="TTarget">The class of the target.</typeparam>
        public static DuckFactory Create<TTarget>(TTarget target) where TTarget : class => new TypedDuckFactory<TTarget>
        (
            Ensure.Parameter.IsNotNull(target, nameof(target))
        );

        private sealed class TypedDuckFactory<TTarget> : DuckFactory where TTarget : class
        {
            public TTarget Target { get; }

            public TypedDuckFactory(TTarget target) => Target = target;

            public override TInterface Like<TInterface>() where TInterface : class
            {
                //
                // Kell egyaltalan proxy-t letrhoznunk?
                //

                if (Target is TInterface possibleResult)
                    return possibleResult;

                return (TInterface) DuckGenerator<TInterface, TTarget>
                    .GetGeneratedType()
                    .CreateInstance(new[] { typeof(TTarget) }, Target);
            }
        }
    }
}