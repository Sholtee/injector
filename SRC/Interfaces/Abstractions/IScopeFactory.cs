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
        /// <param name="tag">User defined data to be bound to the scope. You can read this value back via the <see cref="IHasTag"/> interface.</param>
        IInjector CreateScope(object? tag = null);

        /// <summary>
        /// Options applied on the newly created scopes.
        /// </summary>
        ScopeOptions Options { get; }
    }
}
