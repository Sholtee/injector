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
        /// Services having <see cref="Transient"/> lifetime are instantiated on every request. The caller must dispose them (if necessary). 
        /// </summary>
        Transient = 0,

        /// <summary>
        /// Services having <see cref="Scoped"/> liftime are instantiated only once (per injector) on the first request and disposed automatically when the parent injector is disposed.
        /// </summary>
        Scoped,

        /// <summary>
        /// Services having <see cref="Singleton"/> liftime are instantiated only once (per declaring container) on the first request and disposed automatically when the declaring container is disposed.
        /// </summary>
        Singleton
    }
}