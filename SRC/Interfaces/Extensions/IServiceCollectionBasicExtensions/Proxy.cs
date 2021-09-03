/********************************************************************************
* Proxy.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;
 
    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Hooks into the instantiating process to let you decorate the original service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of the decorator function.</param>
        /// <remarks>You can't create proxies against generic or instance entries. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IModifiedServiceCollection AddProxy(this IModifiedServiceCollection self, Func<IInjector, Type, object, object> decorator)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (decorator is null)
                throw new ArgumentNullException(nameof(decorator));

            if (self.LastEntry is not ISupportsProxying setter || setter.Factory is null)
                //
                // Generikus szerviz, Abstract(), Instance() eseten a metodus nem ertelmezett.
                //

                throw new InvalidOperationException(Resources.PROXYING_NOT_SUPPORTED);

            //
            // Bovitjuk a hivasi lancot a decorator-al.
            //

            Func<IInjector, Type, object> oldFactory = setter.Factory;

            setter.Factory = (injector, type) => decorator(injector, type, oldFactory(injector, type));

            return self;
        }
    }
}