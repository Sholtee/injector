/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI
{
    /// <summary>
    /// Describes the lifetime of a service. A service can be registered in various ways. For more information check out the <see cref="IInjector"/> interface.
    /// </summary>
    public enum Lifetime
    {
        /// <summary>
        /// Transient lifetime. Services having <see cref="Transient"/> lifetime are instantiated on every request and must be disposed (if necessary) by the caller. 
        /// </summary>
        Transient = 0,

        /// <summary>
        /// Singleton lifetime. Services having <see cref="Singleton"/> liftime are instantiated only once on the first request and disposed automatically when the injector itself is disposed.
        /// </summary>
        Singleton
    }
}