/********************************************************************************
* ObjectExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Proxy
{
    /// <summary>
    /// Defines the base class for duck typing (for classes).
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Marks a <typeparamref name="TTarget"/> instance for duck typing.
        /// </summary>
        /// <typeparam name="TTarget">The type of the target (must be a class).</typeparam>
        /// <param name="target">The target instance to which the proxy will be generated.</param>
        /// <returns>The proxy object.</returns>
        public static DuckFactory<TTarget> Act<TTarget>(this TTarget target) where TTarget : class
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return new DuckFactory<TTarget>(target);
        }
    }

    /// <summary>
    /// Defines the base class for duck typing (for structs).
    /// </summary>
    public static class StructExtensions
    {
        /// <summary>
        /// Marks a <typeparamref name="TTarget"/> instance for duck typing.
        /// </summary>
        /// <typeparam name="TTarget">The type of the target (must be a struct).</typeparam>
        /// <param name="target">The target instance to which the proxy will be generated.</param>
        /// <returns>The proxy object.</returns>
        public static DuckFactory<TTarget> Acts<TTarget>(this TTarget target) where TTarget : struct => new DuckFactory<TTarget>(target);
    }
}
