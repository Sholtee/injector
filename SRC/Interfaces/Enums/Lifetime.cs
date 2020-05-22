/********************************************************************************
* Lifetime.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the lifetime of a service.
    /// </summary>
    [SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "The 'Lifetime' namepsace exists only in .NET Framework which won't be targeted.")]
    public enum Lifetime
    {
        /// <summary>
        /// Services having <see cref="Transient"/> lifetime are instantiated on every request and released automatically when the parent <see cref="IInjector"/> is disposed.
        /// </summary>
        Transient = 0,

        /// <summary>
        /// Services having <see cref="Scoped"/> liftime are instantiated only once (per <see cref="IInjector"/>) on the first request and disposed automatically when the parent <see cref="IInjector"/> is disposed.
        /// </summary>
        Scoped,

        /// <summary>
        /// Services having <see cref="Singleton"/> liftime are instantiated only once (in declaring <see cref="IServiceContainer"/>) on the first request and disposed automatically when the container is disposed.
        /// </summary>
        Singleton
    }
}