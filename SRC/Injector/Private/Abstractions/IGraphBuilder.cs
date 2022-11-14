/********************************************************************************
* IGraphBuilder.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Solti.Utils.DI.Interfaces;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Represents the contract of dependency graph builders.
    /// </summary>
    internal interface IGraphBuilder
    {
        void Build(AbstractServiceEntry entry);
    }
}
