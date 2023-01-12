/********************************************************************************
* IServiceEntryBuilder.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// Represents the contract how to build service entries.
    /// </summary>
    internal interface IServiceEntryBuilder
    {
        IFactoryVisitor[] Visitors { get; }

        IBuildContext BuildContext { get; }

        void Build(AbstractServiceEntry entry);
    }
}
