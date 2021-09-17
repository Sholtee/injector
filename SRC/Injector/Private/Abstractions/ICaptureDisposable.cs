/********************************************************************************
* ICaptureDisposable.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal interface ICaptureDisposable
    {
        void CaptureDisposable(object obj);
    }
}
