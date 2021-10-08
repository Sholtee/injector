/********************************************************************************
* Proxy.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;
    using Proxy;

    public static partial class IServiceCollectionAdvancedExtensions
    {
        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <typeparam name="TInterceptor">The interceptor class.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see remarks).</exception>
        public static IModifiedServiceCollection WithProxy<TInterceptor>(this IModifiedServiceCollection self) => self.WithProxy(typeof(TInterceptor));

        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation). The easiest way to decorate an instance is using the <see cref="InterfaceInterceptor{TInterface}"/> class.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="interceptor">The interceptor class.</param>
        /// <remarks>You can't create proxies against instances or not owned entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see remarks).</exception>
        public static IModifiedServiceCollection WithProxy(this IModifiedServiceCollection self, Type interceptor)
        {
            //
            // A tobbit a ProxyGenerator<> ellenorzi
            //

            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (interceptor is null)
                throw new ArgumentNullException(nameof(interceptor));

            self.LastEntry.ApplyInterceptor(interceptor);

            return self;
        }
    }
}