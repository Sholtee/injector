/********************************************************************************
* IServiceEntryVisitor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IServiceEntryVisitor
    {
        void Visit(AbstractServiceEntry entry);
    }
}
