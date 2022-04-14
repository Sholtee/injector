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
    public interface IScopeFactory: IDisposableEx
    {
        /// <summary>
        /// Creates an independent new scope
        /// </summary>
        /// <param name="lifetime">Optional hint specifying the object that is responsible for releasing the newly created scope.</param>
        IInjector CreateScope(object? lifetime = null);

        /// <summary>
        /// Options applied on the newly created scopes.
        /// </summary>
        ScopeOptions Options { get; }
    }
}
