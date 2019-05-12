/********************************************************************************
* IInjector.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using JetBrains.Annotations;

namespace Solti.Utils.Injector
{
    public interface IInjector: IDisposable
    {
        IDecorator Register([NotNull] Type iface, [NotNull] Type implementation, DependencyType type = DependencyType.Transient);

        IDecorator<TInterface> Register<TInterface, TImplementation>(DependencyType type = DependencyType.Transient);

        /// <summary>
        /// Resolves a dependency (thread safe).
        /// </summary>
        object Get([NotNull] Type iface);

        TInterface Get<TInterface>();
    }
}