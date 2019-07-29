/********************************************************************************
* Disposable.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Implements the <see cref="IDisposable"/> interface.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    public class Disposable: IDisposable
    {
        /// <summary>
        /// Indicates whether the object was disposed or not.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Method to be overridden to implement custom logic.
        /// </summary>
        /// <param name="disposeManaged">It is set to true on <see cref="IDisposable.Dispose"/> call.</param>
        protected virtual void Dispose(bool disposeManaged)
        {
        }

        /// <summary>
        /// Checks whether the object was disposed and throws if yes.
        /// </summary>
        protected void CheckDisposed()
        {
            if (Disposed) throw AlreadyDisposedException;
        }

        ~Disposable() => Dispose(disposeManaged: false);
 
        public void Dispose()
        {
            CheckDisposed();

            Dispose(disposeManaged: true);
            GC.SuppressFinalize(this);

            Disposed = true;
        }

        internal static readonly InvalidOperationException AlreadyDisposedException = new InvalidOperationException(Resources.ALREADY_DISPOSED);
    }
}
