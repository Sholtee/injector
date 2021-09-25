/********************************************************************************
* IServicePath.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the path in which the current request takes place.
    /// </summary>
    /// <remarks>The last entry describes the service that is currently being requested.</remarks>
    public interface IServicePath: IReadOnlyList<AbstractServiceEntry>
    {
        /// <summary>
        /// Throws if the path is circular.
        /// </summary>
        void CheckNotCircular();
    }
}
