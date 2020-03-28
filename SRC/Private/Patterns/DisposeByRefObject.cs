/********************************************************************************
* DisposeByRefObject.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Manages object lifetime by refence counting.
    /// </summary>
    public class DisposeByRefObject : Disposable, IDisposeByRef
    {
        private readonly object FLock = new object();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureNotDisposed() 
        {
            if (RefCount == 0)
                throw new ObjectDisposedException(null);
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
            lock (FLock)
            {
                EnsureNotDisposed();
                return ++RefCount;
            }
        }

        /// <summary>
        /// Decrements the reference counter as an atomic operation and disposes the object if the reference count reaches the zero.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int Release()
        {
            lock (FLock) 
            {
                EnsureNotDisposed();
                if (--RefCount > 0) return RefCount;
            }

            Dispose();
            return 0;
        }

        /// <summary>
        /// Decrements the reference counter as an atomic operation and disposes the object asynchronously if the reference count reached the zero.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public async Task<int> ReleaseAsync() 
        {
            lock (FLock) 
            {
                EnsureNotDisposed();
                if (--RefCount > 0) return RefCount;
            }

            await DisposeAsync();
            return 0;
        }
    }
}
