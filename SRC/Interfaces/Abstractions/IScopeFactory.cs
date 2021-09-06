/********************************************************************************
* IScopeFactory.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Interfaces
{
    using Primitives.Patterns;

    /// <summary>
    /// Provides a thread safe way to create new scopes.
    /// </summary>
    /// <remarks>Disposing the factory destroys all the living child scopes.</remarks>
    public interface IScopeFactory: IDisposableEx
    {
        /// <summary>
        /// Creates an independent new scope
        /// </summary>
        IInjector CreateScope();

        /// <summary>
        /// Options applied on the newly created scopes.
        /// </summary>
        ScopeOptions ScopeOptions { get; }
    }
}
