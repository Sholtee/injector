/********************************************************************************
* IBuildContext.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Context used during the build phase.
    /// </summary>
    public interface IBuildContext
    {
        /// <summary>
        /// Creates a new slot and return its ID
        /// </summary>
        int AssignSlot();

        /// <summary>
        /// The compiler to be used to build <see cref="Expression"/>s.
        /// </summary>
        IDelegateCompiler Compiler { get; }
    }
}
