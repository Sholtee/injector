/********************************************************************************
* IReferenceCounted.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal interface IReferenceCounted
    {
        int AddRef();
        int Release();
    }
}
