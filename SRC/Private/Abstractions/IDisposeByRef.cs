/********************************************************************************
* IDisposeByRef.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal interface IDisposeByRef: IDisposable
    {
        int AddRef();
        int Release();
    }
}
