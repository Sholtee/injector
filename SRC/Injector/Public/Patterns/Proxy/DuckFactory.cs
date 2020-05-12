/********************************************************************************
* DuckFactory.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy
{
    using Generators;

    using DI.Internals;

    /// <summary>
    /// Generates duck typing proxy objects.
    /// </summary>
    /// <typeparam name="TTarget">The class of the target.</typeparam>
    public class DuckFactory<TTarget> where TTarget: class
    {
        internal TTarget Target { get; }

        /// <summary>
        /// Creates a new <see cref="DuckFactory{TTarget}"/> instance against the given <paramref name="target"/>.
        /// </summary>
        /// <param name="target">The target of this factory.</param>
        public DuckFactory(TTarget target) =>
            Target = Ensure.Parameter.IsNotNull(target, nameof(target));

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

            return (TInterface) DuckGenerator<TInterface, TTarget>
                .GeneratedType
                .CreateInstance(new[] {typeof(TTarget)}, Target);
        }
    }
}