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
    public interface IServicePath: IEnumerable<IServiceReference>
    {
        /// <summary>
        /// The first pushed <see cref="IServiceReference"/>.
        /// </summary>
        IServiceReference? First { get; }

        /// <summary>
        /// The last pushed <see cref="IServiceReference"/>.
        /// </summary>
        IServiceReference? Last { get; }

        /// <summary>
        /// Appends the path with the given <see cref="IServiceReference"/>.
        /// </summary>
        void Push(IServiceReference node);

        /// <summary>
        /// Removes the <see cref="Last"/> pushed <see cref="IServiceReference"/>.
        /// </summary>
        void Pop();

        /// <summary>
        /// Throws if the path is circular.
        /// </summary>
        void CheckNotCircular();
    }
}
