/********************************************************************************
* ICaptureDisposable.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal interface ICaptureDisposable
    {
        IReadOnlyCollection<object> CapturedDisposables { get; }
    }
}
