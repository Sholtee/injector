/********************************************************************************
* IGraphBuilder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Solti.Utils.DI.Interfaces;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Builds the dependency graph.
    /// </summary>
    internal interface IGraphBuilder
    {
        void Build(AbstractServiceEntry entry);

        IServiceEntryLookup Lookup { get; }

        int Slots { get; }
    }
}
