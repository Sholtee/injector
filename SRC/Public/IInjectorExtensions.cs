/********************************************************************************
* IInjectorExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    public static class IInjectorExtensions
    {
        /// <summary>
        /// Registers a new service with the given type.
        /// </summary>
        public static IInjector Service<TInterface, TImplementation>(this IInjector self, Lifetime lifetime = Lifetime.Transient)
        {
            return self.Service(typeof(TInterface), typeof(TImplementation), lifetime);
        }

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request.
        /// </summary>
        public static IInjector Lazy<TInterface>(this IInjector self, IResolver resolver, Lifetime lifetime = Lifetime.Transient)
        {
            return self.Lazy(typeof(TInterface), resolver, lifetime);
        }

        /// <summary>
        /// Registers a new service factory with the given type.
        /// </summary>
        public static IInjector Factory<TInterface>(this IInjector self, Func<IInjector, TInterface> factory, Lifetime lifetime = Lifetime.Transient)
        {
            return self.Factory(typeof(TInterface), (me, type) => factory(me), lifetime);
        }

        /// <summary>
        /// Hooks into the instantiating process.
        /// </summary>
        public static IInjector Proxy<TInterface>(this IInjector self, Func<IInjector, TInterface, TInterface> decorator)
        {
            return self.Proxy(typeof(TInterface), (me, type, instance) => decorator(me, (TInterface) instance));
        }

        /// <summary>
        /// Registers a pre-created instance.
        /// </summary>
        public static IInjector Instance<TInterface>(this IInjector self, TInterface instance, bool releaseOnDispose = true)
        {
            return self.Instance(typeof(TInterface), instance, releaseOnDispose);
        }

        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <remarks>You can call it from different threads, parallelly.</remarks>
        public static TInterface Get<TInterface>(this IInjector self)
        {
            return (TInterface) self.Get(typeof(TInterface));
        }
    }
}