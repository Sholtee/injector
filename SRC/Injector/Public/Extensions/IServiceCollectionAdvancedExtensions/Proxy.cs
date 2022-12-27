/********************************************************************************
* Proxy.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Interfaces.Properties;
    using Internals;

    public static partial class IServiceCollectionAdvancedExtensions
    {
        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <remarks>You can't create proxies against instances and open generic services. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying is not allowed (see remarks).</exception>
        public static IModifiedServiceCollection UsingProxy<TInterceptor>(this IModifiedServiceCollection self) where TInterceptor: IInterfaceInterceptor
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (self.LastEntry is not ProducibleServiceEntry pse)
                throw new NotSupportedException(Resources.PROXYING_NOT_SUPPORTED);

            pse.ApplyProxy
            (
                //
                // Proxies registered by this way always target the service interface.
                //

                ServiceActivator.InterceptorsToProxyDelegate(pse.Interface, pse.Interface, new Type[]
                {
                    typeof(TInterceptor)
                })
            );

            return self;
        }
    }
}