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
        IInjector Service([NotNull] Type iface, [NotNull] Type implementation, DependencyType type = DependencyType.Transient);

        /// <summary>
        /// Registers a new service with the given type.
        /// </summary>
        IInjector Service<TInterface, TImplementation>(DependencyType type = DependencyType.Transient);

        /// <summary>
        /// Registers a new service factory with the given type.
        /// </summary>
        IInjector Factory([NotNull] Type iface, [NotNull] Func<IInjector, Type, object> factory, DependencyType type = DependencyType.Transient);

        /// <summary>
        /// Registers a new service factory with the given type.
        /// </summary>
        IInjector Factory<TInterface>([NotNull] Func<IInjector, TInterface> factory, DependencyType type = DependencyType.Transient);

        /// <summary>
        /// Hooks into the instantiating process.
        /// </summary>
        IInjector Proxy([NotNull] Type iface, [NotNull] Func<IInjector, Type, object, object> factory);

        /// <summary>
        /// Hooks into the instantiating process.
        /// </summary>
        IInjector Proxy<TInterface>([NotNull] Func<IInjector, TInterface, TInterface> factory);

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
    }
}