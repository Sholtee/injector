/********************************************************************************
* DisposeByRefObject.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Manages object lifetime by refence counting.
    /// </summary>
    public class DisposeByRefObject : Disposable, IDisposeByRef
    {
        //
        // Nem lehet "await" "lock" blokkon belul -> Semaphore
        //

        private readonly SemaphoreSlim FLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Disposes this <see cref="DisposeByRefObject"/> instance.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) FLock.Dispose();
            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Disposes this <see cref="DisposeByRefObject"/> instance asynchronously.
        /// </summary>
        protected override ValueTask AsyncDispose()
        {
            FLock.Dispose();
            return default;

            //
            // Ne elgyen "base" hivas mert az a "Dispose"-t hivna
            //
        }

        /// <summary>
        /// The current reference count.
        /// </summary>
        public int RefCount { get; private set; } = 1;

        /// <summary>
        /// Increments the reference counter as an atomic operation.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int AddRef()
        {
            Ensure.NotDisposed(this);

            //
            // Ha vki itt varakozik mikor a Semaphore felszabaditasra kerul akkor ObjectDisposedException-t kap
            //

            FLock.Wait();
            try
            {
                return ++RefCount;
            }
            finally
            {
                FLock.Release();
            }
        }

        /// <summary>
        /// Decrements the reference counter as an atomic operation and disposes the object if the reference count reaches the zero.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int Release()
        {
            Ensure.NotDisposed(this);

            FLock.Wait();
            try
            {
                if (--RefCount == 0) Dispose();
                return RefCount;
            }
            finally 
            {
                //
                // Dispose() hivas felszabaditja a Semaphore-t is
                //

                if (!Disposed) FLock.Release();
            }
        }

        /// <summary>
        /// Decrements the reference counter as an atomic operation and disposes the object asynchronously if the reference count reaches the zero.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public async Task<int> ReleaseAsync() 
        {
            Ensure.NotDisposed(this);

            await FLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (--RefCount == 0) await DisposeAsync();
                return RefCount;
            }
            finally
            {
                //
                // Dispose() hivas felszabaditja a Semaphore-t is
                //

                if (!Disposed) FLock.Release();
            }
        }
    }
}
