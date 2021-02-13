/********************************************************************************
* IServiceGraph.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the service graph in which the current request takes place.
    /// </summary>
    [SystemService]
    public interface IServiceGraph: IEnumerable<IServiceReference>
    {
        /// <summary>
        /// The requestor of the service.
        /// </summary>
        IServiceReference? Requestor { get; }

        /// <summary>
        /// Appends the graph with the given <see cref="IServiceReference"/>.
        /// </summary>
        IDisposable With(IServiceReference reference);

        /// <summary>
        /// Throws if the graph is circular.
        /// </summary>
        void CheckNotCircular();

        /// <summary>
        /// Creates a new node.
        /// </summary>
        IServiceGraph CreateNode();

        /// <summary>
        /// Clears the graph.
        /// </summary>
        void Clear();

        /// <summary>
        /// Creates a root node.
        /// </summary>
        IServiceGraph CreateRoot();
    }
}
