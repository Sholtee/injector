/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI
{
    using Internals;

    /// <summary>
    /// Describes the lifetime of a service. A service can be registered in various ways. For more information check out the <see cref="IServiceContainerExtensions"/> class.
    /// </summary>
    public enum Lifetime
    {
        /// <summary>
        /// Services having <see cref="Transient"/> lifetime are instantiated on every request and released automatically when the parent <see cref="IInjector"/> is disposed. Note that the caller may also free the requested services (the system can handle it properly).
        /// </summary>
        [RelatedEntryKind(typeof(TransientServiceEntry))]
        Transient = 0,

        /// <summary>
        /// Services having <see cref="Scoped"/> liftime are instantiated only once (per <see cref="IInjector"/>) on the first request and disposed automatically when the parent <see cref="IInjector"/> is disposed.
        /// </summary>
        [RelatedEntryKind(typeof(ScopedServiceEntry))]
        Scoped,

        /// <summary>
        /// Services having <see cref="Singleton"/> liftime are instantiated only once (in declaring <see cref="IServiceContainer"/>) on the first request and disposed automatically when the container is disposed.
        /// </summary>
        [RelatedEntryKind(typeof(SingletonServiceEntry))]
        Singleton
    }
}