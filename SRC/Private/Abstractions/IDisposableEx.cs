/********************************************************************************
* IDisposableEx.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal interface IDisposableEx: IDisposable
    {
        bool Disposed { get; }
    }
}