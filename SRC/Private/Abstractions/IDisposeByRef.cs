/********************************************************************************
* IDisposeByRef.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    internal interface IDisposeByRef: IDisposable
    {
        int AddRef();
        int Release();
        Task<int> ReleaseAsync();
    }
}
