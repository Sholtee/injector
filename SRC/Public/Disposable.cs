/********************************************************************************
* Disposable.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    public class Disposable: IDisposable
    {
        protected virtual void Dispose(bool disposeManaged)
        {
        }

        ~Disposable()
        {
            Dispose(disposeManaged: false);
        }

        void IDisposable.Dispose()
        {
            Dispose(disposeManaged: true);
            GC.SuppressFinalize(this);
        }
    }
}
