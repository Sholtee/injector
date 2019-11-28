/********************************************************************************
* IReferenceCounted.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal interface IReferenceCounted: IDisposable
    {
        int AddRef();
        int Release();
    }
}
