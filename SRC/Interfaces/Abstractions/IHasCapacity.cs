/********************************************************************************
* IHasCapacity.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Provides the mechanism for adjusting capacity.
    /// </summary>
    public interface IHasCapacity
    {
        /// <summary>
        /// The capacity.
        /// </summary>
        int Capacity { get; set; }
    }
}
