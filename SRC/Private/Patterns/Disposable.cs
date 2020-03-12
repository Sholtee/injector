/********************************************************************************
* Disposable.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Implements the <see cref="IDisposable"/> interface.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    public class Disposable : IDisposableEx
#if !NETSTANDARD1_6
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// Indicates whether the object was disposed or not.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Method to be overridden to implement custom disposal logic.
        /// </summary>
        /// <param name="disposeManaged">It is set to true on <see cref="IDisposable.Dispose"/> call.</param>
        protected virtual void Dispose(bool disposeManaged) => Debug.WriteLineIf(!disposeManaged, $"{GetType()} is disposed by GC. You may be missing a Dispose() call.");

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources asynchronously
        /// </summary>
        protected virtual async ValueTask AsyncDispose() => await Task.Run(() => Dispose(true)).ConfigureAwait(false);

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
            Ensure.NotDisposed(this);

            Dispose(disposeManaged: true);
            GC.SuppressFinalize(this);

            Disposed = true;
        }

        private int FDisposing;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "The method implements the dispose pattern.")]
        public async ValueTask DisposeAsync()
        {
            //
            // MSDN szerint itt nem dobhatunk ObjectDisposedException kivetelt.
            //

            if (Interlocked.Exchange(ref FDisposing, 1) == 1) return;

            await AsyncDispose();

            GC.SuppressFinalize(this);
            Disposed = true;
        }
    }
}
