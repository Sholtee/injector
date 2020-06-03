/********************************************************************************
* ObjectExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.Proxy
{
    using DI.Internals;

    /// <summary>
    /// Defines the base class for duck typing.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Marks a <typeparamref name="TTarget"/> instance for duck typing.
        /// </summary>
        /// <typeparam name="TTarget">The type of the target.</typeparam>
        /// <param name="target">The target instance to which the proxy will be generated.</param>
        /// <returns>The <see cref="DuckFactory"/> instance for the <paramref name="target"/>.</returns>
        public static DuckFactory Act<TTarget>(this TTarget target) where TTarget : class => DuckFactory.Create
        (
            Ensure.Parameter.IsNotNull(target, nameof(target))
        );
    }
}
