/********************************************************************************
* IServiceGraph.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Gets the current service graph in which the request takes place.
    /// </summary>
    public interface IServiceGraph: IEnumerable<IServiceReference>
    {
        /// <summary>
        /// The requestor of the service.
        /// </summary>
        IServiceReference? Requestor { get; }
    }
}
