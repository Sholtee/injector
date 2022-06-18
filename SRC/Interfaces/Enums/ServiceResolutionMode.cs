/********************************************************************************
* ServiceResolutionMode.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies when the system should build the dependency graph. 
    /// </summary>
    public enum ServiceResolutionMode
    {
        /// <summary>
        /// Just In Time
        /// </summary>
        JIT,

        /// <summary>
        /// Ahead Of Time
        /// </summary>
        AOT
    }
}
