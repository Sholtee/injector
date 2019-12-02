/********************************************************************************
* Disposable.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Implements the <see cref="IDisposable"/> interface.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    public class Disposable: IDisposableEx
    {
        /// <summary>
        /// Indicates whether the object was disposed or not.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Method to be overridden to implement custom disposal logic.
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
            if (Disposed) throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Destructor of this class.
        /// </summary>
        ~Disposable() => Dispose(disposeManaged: false);

        /// <summary>
        /// Implements the <see cref="IDisposable.Dispose"/> method.
        /// </summary>
        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "The method is implemented correctly.")]
        public void Dispose()
        {
            CheckDisposed();

            Dispose(disposeManaged: true);
            GC.SuppressFinalize(this);

            Disposed = true;
        }
    }
}
