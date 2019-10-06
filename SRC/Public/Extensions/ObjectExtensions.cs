/********************************************************************************
* ObjectExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Proxy
{
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
        /// <returns>The <see cref="DuckFactory{TTarget}"/> instance for the <paramref name="target"/>.</returns>
        public static DuckFactory<TTarget> Act<TTarget>(this TTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return new DuckFactory<TTarget>(target);
        }
    }
}
