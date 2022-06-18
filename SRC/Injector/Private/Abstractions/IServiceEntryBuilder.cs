/********************************************************************************
* IServiceEntryBuilder.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IServiceEntryBuilder
    {
        void Build(AbstractServiceEntry entry);
    }
}
