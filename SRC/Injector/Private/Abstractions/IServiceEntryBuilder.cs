/********************************************************************************
* IServiceEntryBuilder.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// Represents the contract how to build service entries.
    /// </summary>
    internal interface IServiceEntryBuilder
    {
        IReadOnlyList<IFactoryVisitor> Visitors { get; }

        IBuildContext BuildContext { get; }

        void Init(IEnumerable<AbstractServiceEntry> entries);

        void Build(AbstractServiceEntry entry);
    }
}
