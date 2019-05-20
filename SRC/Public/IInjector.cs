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
        IInjector Service([Expect(typeof(NotNull)), Expect(typeof(IsInterface))] Type iface, [Expect(typeof(NotNull)), Expect(typeof(IsClass))] Type implementation, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Registers a new service factory with the given type.
        /// </summary>
        IInjector Factory([Expect(typeof(NotNull)), Expect(typeof(IsInterface))] Type iface, [Expect(typeof(NotNull))] Func<IInjector, Type, object> factory, Lifetime lifetime = Lifetime.Transient);

        /// <summary>
        /// Hooks into the instantiating process.
        /// </summary>
        IInjector Proxy([Expect(typeof(NotNull)), Expect(typeof(IsInterface))] Type iface, [Expect(typeof(NotNull))] Func<IInjector, Type, object, object> decorator);

        /// <summary>
        /// Registers a pre-created instance.
        /// </summary>
        IInjector Instance([Expect(typeof(NotNull)), Expect(typeof(IsInterface))] Type iface, [Expect(typeof(NotNull))] object instance);

        /// <summary>
        /// Resolves a dependency.
        /// </summary>
        /// <remarks>You can call it from different threads, parallelly.</remarks>
        object Get([Expect(typeof(NotNull)), Expect(typeof(IsInterface))] Type iface);

        /// <summary>
        /// Creates a child injector that inherits all the entries of its parent.
        /// </summary>
        IInjector CreateChild();
    }
}