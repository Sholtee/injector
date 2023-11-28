/********************************************************************************
* Get.cs                                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    public static partial class IInjectorBasicExtensions
    {
        /// <summary>
        /// Gets the service instance associated with the given type and (optional) key:
        /// <code>IMyService svc = scope.Get&lt;IMyService&gt;();</code>
        /// </summary>
        /// <typeparam name="TType">The "id" of the service to be resolved.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="key">The (optional) service key.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        public static TType Get<TType>(this IInjector self, object? key = null) where TType : class
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return (TType) self.Get(typeof(TType), key);
        }

        /// <summary>
        /// Tries to get the service instance associated with the given type and (optional) key:
        /// <code>IMyService? svc = scope.TryGet&lt;IMyService&gt;();</code>
        /// </summary>
        /// <typeparam name="TType">The "id" of the service to be resolved.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="key">The (optional) service key.</param>
        /// <returns>The requested service instance if the resolution was successful, null otherwise.</returns>
        public static TType? TryGet<TType>(this IInjector self, object? key = null) where TType : class
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            return (TType?) self.TryGet(typeof(TType), key);
        }
    }
}