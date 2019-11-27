/********************************************************************************
* DuckFactory.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Proxy
{
    using Properties;
    using Internals;

    /// <summary>
    /// Generates duck typing proxy objects.
    /// </summary>
    /// <typeparam name="TTarget">The class of the target.</typeparam>
    public class DuckFactory<TTarget>
    {
        internal TTarget Target { get; }

        /// <summary>
        /// Creates a new <see cref="DuckFactory{TTarget}"/> instance against the given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target of this factory.</param>
        /// <exception cref="ArgumentException"><typeparamref name="TTarget"/> is not a class.</exception>
        public DuckFactory(TTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (!typeof(TTarget).IsClass())
                throw new ArgumentException(Resources.NOT_A_CLASS);

            Target = target;
        }

        /// <summary>
        /// Generates a proxy object to let the target behave like a <typeparamref name="TInterface"/> instance.
        /// </summary>
        /// <remarks><typeparamref name="TTarget"/> must declare all the <typeparamref name="TInterface"/> members publicly.</remarks>
        /// <returns>The newly created proxy object.</returns>
        public TInterface Like<TInterface>() where TInterface : class
        {
            //
            // Kell egyaltalan proxy-t letrhoznunk?
            //

            if (Target is TInterface possibleResult)
                return possibleResult;

            return (TInterface) GeneratedDuck<TInterface, TTarget>
                .Type
                .CreateInstance(new[] {typeof(TTarget)}, Target);
        }
    }
}