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
        IInjector Service([NotNull] Type iface, [NotNull] Type implementation, DependencyType type = DependencyType.Transient);

        IInjector Service<TInterface, TImplementation>(DependencyType type = DependencyType.Transient);

        IInjector Factory([NotNull] Type iface, [NotNull] Func<IInjector, Type, object> factory, DependencyType type = DependencyType.Transient);

        IInjector Factory<TInterface>([NotNull] Func<IInjector, TInterface> factory, DependencyType type = DependencyType.Transient);

        IInjector Proxy([NotNull] Type iface, [NotNull] Func<IInjector, Type, object, object> factory);

        IInjector Proxy<TInterface>([NotNull] Func<IInjector, TInterface, TInterface> factory);

        /// <summary>
        /// Resolves a dependency (thread safe).
        /// </summary>
        object Get([NotNull] Type iface);

        TInterface Get<TInterface>();
    }
}