/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI
{
    using Internals;

    /// <summary>
    /// Describes the lifetime of a service. A service can be registered in various ways. For more information check out the <see cref="IServiceContainerExtensions"/> class.
    /// </summary>
    [SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "The 'Lifetime' namepsace exists only in .NET Framework which won't be targeted.")]
    public enum Lifetime
    {
        //
        // Az int ertekek szamitanak, ne valtoztassuk meg oket!
        //

        /// <summary>
        /// Services having <see cref="Transient"/> lifetime are instantiated on every request and released automatically when the parent <see cref="IInjector"/> is disposed.
        /// </summary>
        [RelatedEntryKind(typeof(TransientServiceEntry))]
        Transient = 0,

        /// <summary>
        /// Services having <see cref="Scoped"/> liftime are instantiated only once (per <see cref="IInjector"/>) on the first request and disposed automatically when the parent <see cref="IInjector"/> is disposed.
        /// </summary>
        [RelatedEntryKind(typeof(ScopedServiceEntry))]
        Scoped = 1,

        /// <summary>
        /// Services having <see cref="Singleton"/> liftime are instantiated only once (in declaring <see cref="IServiceContainer"/>) on the first request and disposed automatically when the container is disposed.
        /// </summary>
        [RelatedEntryKind(typeof(SingletonServiceEntry))]
        Singleton = 2
    }
}