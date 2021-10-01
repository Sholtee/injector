/********************************************************************************
* IPathAccess.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IPathAccess
    {
        IServicePath Path { get; }
    }
}
