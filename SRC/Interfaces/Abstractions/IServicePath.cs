/********************************************************************************
* IServicePath.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the path in which the current request takes place.
    /// </summary>
    public interface IServicePath: IEnumerable<IServiceReference>
    {
        /// <summary>
        /// The last pushed service.
        /// </summary>
        IServiceReference? Requestor { get; }

        /// <summary>
        /// Appends the path with the given <see cref="IServiceReference"/>.
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        IDisposable With(IServiceReference reference);

        /// <summary>
        /// Throws if the path is circular.
        /// </summary>
        void CheckNotCircular();
    }
}
