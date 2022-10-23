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
        /// <remarks>Useful if you're planning to mock the <see cref="IInjector"/> invocations.</remarks>
        JIT,

        /// <summary>
        /// Ahead Of Time
        /// </summary>
        AOT
    }
}
