/********************************************************************************
* IHasUnderlyingImplementation.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Exposes the underlying implementation of a service. 
    /// </summary>
    public interface IHasUnderlyingImplementation
    {
        /// <summary>
        /// The underlying implementation of the service.
        /// </summary>
        object? UnderlyingImplementation { get; }
    }
}
