/********************************************************************************
* IInjector.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    public interface IInjector: IDisposable
    {
        /// <summary>
        /// Registers a new service with the given type.
        /// </summary>
        IInjector Service([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull), typeof(Class))] Type implementation, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Registers a new service factory with the given type.
        /// </summary>
        IInjector Factory([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull))] Func<IInjector, Type, object> factory, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Hooks into the instantiating process.
        /// </summary>
        IInjector Proxy([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull))] Func<IInjector, Type, object, object> decorator);

        /// <summary>
        /// Registers a pre-created instance.
        /// </summary>
        IInjector Instance([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull))] object instance);

        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <remarks>You can call it from different threads, parallelly.</remarks>
        object Get([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface);

        /// <summary>
        /// Creates a child injector that inherits all the entries of its parent.
        /// </summary>
        IInjector CreateChild();
    }
}