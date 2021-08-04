/********************************************************************************
* IWrapped.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Represents a wrapped object.
    /// </summary>
    public interface IWrapped
    {
        /// <summary>
        /// Gets the unwrapped object.
        /// </summary>
        /// <remarks>This is an immutable property.</remarks>
        object UnderlyingObject { get; }
    }
}
