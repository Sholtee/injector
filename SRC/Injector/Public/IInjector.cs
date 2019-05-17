/********************************************************************************
* IInjector.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using JetBrains.Annotations;

namespace Solti.Utils.DI
{
    public interface IInjector: IDisposable
    {
        /// <summary>
        /// Registers a new service with the given type.
        /// </summary>
        IInjector Service([NotNull] Type iface, [NotNull] Type implementation, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Registers a new service with the given type.
        /// </summary>
        IInjector Service<TInterface, TImplementation>(Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Registers a new service factory with the given type.
        /// </summary>
        IInjector Factory([NotNull] Type iface, [NotNull] Func<IInjector, Type, object> factory, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Registers a new service factory with the given type.
        /// </summary>
        IInjector Factory<TInterface>([NotNull] Func<IInjector, TInterface> factory, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Hooks into the instantiating process.
        /// </summary>
        IInjector Proxy([NotNull] Type iface, [NotNull] Func<IInjector, Type, object, object> decorator);

        /// <summary>
        /// Hooks into the instantiating process.
        /// </summary>
        IInjector Proxy<TInterface>([NotNull] Func<IInjector, TInterface, TInterface> decorator);

        /// <summary>
        /// Registers a pre-created instance.
        /// </summary>
        IInjector Instance([NotNull] Type iface, [NotNull] object instance);

        /// <summary>
        /// Registers a pre-created instance.
        /// </summary>
        IInjector Instance<TInterface>([NotNull] TInterface instance);

        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <remarks>You can call it from different threads, parallelly.</remarks>
        object Get([NotNull] Type iface);

        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <remarks>You can call it from different threads, parallelly.</remarks>
        TInterface Get<TInterface>();

        /// <summary>
        /// Creates a child injector that inherits all the entries of its parent.
        /// </summary>
        IInjector CreateChild();
    }
}