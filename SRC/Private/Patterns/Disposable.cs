/********************************************************************************
* Disposable.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    public class Disposable: IDisposable
    {
        public bool Disposed { get; private set; }

        protected virtual void Dispose(bool disposeManaged)
        {
        }

        protected void CheckDisposed()
        {
            if (Disposed) throw AlreadyDisposedException;
        }

        ~Disposable() => Dispose(disposeManaged: false);
 

        void IDisposable.Dispose()
        {
            CheckDisposed();

            Dispose(disposeManaged: true);
            GC.SuppressFinalize(this);

            Disposed = true;
        }

        internal static readonly InvalidOperationException AlreadyDisposedException = new InvalidOperationException(Resources.ALREADY_DISPOSED);
    }
}
