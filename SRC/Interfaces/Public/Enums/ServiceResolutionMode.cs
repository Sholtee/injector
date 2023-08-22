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
        /// <remarks>
        /// The assembled resolver function roughly looks like this:
        /// <code>
        /// new Dependant(scope.Get(typeof(Dependency)))
        /// </code>
        /// This approach is useful when you're planning to mock the <see cref="IInjector"/> invocations but not recommended in production due to its performance impact.
        /// </remarks>
        JIT,

        /// <summary>
        /// Ahead Of Time
        /// </summary>
        /// <remarks>
        /// The assembled resolver function roughly looks like this:
        /// <code>
        /// new Dependant(new Dependency())
        /// </code>
        /// </remarks>
        AOT
    }
}
