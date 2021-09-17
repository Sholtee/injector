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
        void CaptureDisposable(object obj);

        IReadOnlyCollection<object> CapturedDisposables { get; }
    }
}
