/********************************************************************************
* IScopeFactory.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Provides a thread safe way to create new scopes.
    /// </summary>
    public interface IScopeFactory
    {
        /// <summary>
        /// Creates an independent new scope
        /// </summary>
        IInjector CreateScope();

        /// <summary>
        /// Creates an independent new scope
        /// </summary>
        IInjector CreateScope(IServiceContainer parent);
    }
}
