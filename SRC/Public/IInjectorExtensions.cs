/********************************************************************************
* IInjectorExtensions.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using JetBrains.Annotations;

namespace Solti.Utils.DI
{
    public static class IInjectorExtensions
    {
        /// <summary>
        /// Registers a new service with the given type.
        /// </summary>
        public static IInjector Service<TInterface, TImplementation>([NotNull] this IInjector self, Lifetime lifetime = Lifetime.Transient)
        {
            return self.Service(typeof(TInterface), typeof(TImplementation), lifetime);
        }

        /// <summary>
        /// Registers a new service factory with the given type.
        /// </summary>
        public static IInjector Factory<TInterface>([NotNull] this IInjector self, [NotNull] Func<IInjector, TInterface> factory, Lifetime lifetime = Lifetime.Transient)
        {
            return self.Factory(typeof(TInterface), (me, type) => factory(me), lifetime);
        }

        /// <summary>
        /// Hooks into the instantiating process.
        /// </summary>
        public static IInjector Proxy<TInterface>([NotNull] this IInjector self, [NotNull] Func<IInjector, TInterface, TInterface> decorator)
        {
            return self.Proxy(typeof(TInterface), (me, type, instance) => decorator(me, (TInterface) instance));
        }

        /// <summary>
        /// Registers a pre-created instance.
        /// </summary>
        public static IInjector Instance<TInterface>([NotNull] this IInjector self, [NotNull] TInterface instance)
        {
            return self.Instance(typeof(TInterface), instance);
        }

        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <remarks>You can call it from different threads, parallelly.</remarks>
        public static TInterface Get<TInterface>([NotNull] this IInjector self)
        {
            return (TInterface) self.Get(typeof(TInterface));
        }
    }
}