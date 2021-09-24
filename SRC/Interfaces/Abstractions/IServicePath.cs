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
    public interface IServicePath: IReadOnlyList<AbstractServiceEntry>
    {
        /// <summary>
        /// The first pushed <see cref="AbstractServiceEntry"/>.
        /// </summary>
        AbstractServiceEntry? First { get; }

        /// <summary>
        /// The last pushed <see cref="AbstractServiceEntry"/>.
        /// </summary>
        AbstractServiceEntry? Last { get; }

        /// <summary>
        /// Throws if the path is circular.
        /// </summary>
        void CheckNotCircular();
    }
}
