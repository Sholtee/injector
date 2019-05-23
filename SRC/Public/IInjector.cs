/********************************************************************************
* IInjector.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Internals;

    public interface IInjector: IComposite<IInjector>
    {
        /// <summary>
        /// Registers a new service with the given type.
        /// </summary>
        IInjector Service([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull), typeof(Class))] Type implementation, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Registers a service where the implementation will be resolved on the first request.
        /// </summary>
        IInjector Lazy([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull))] IResolver resolver, Lifetime lifetime = Lifetime.Transient);

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
        IInjector Instance([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface, [ParameterIs(typeof(NotNull))] object instance, bool releaseOnDispose = true);

        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <remarks>You can call it from different threads, parallelly.</remarks>
        object Get([ParameterIs(typeof(NotNull), typeof(Interface), typeof(NotGeneric))] Type iface);

        /// <summary>
        /// Registered entries.
        /// </summary>
        IReadOnlyList<Type> Entries { get; }
    }
}