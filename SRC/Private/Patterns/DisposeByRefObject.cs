/********************************************************************************
* DisposeByRefObject.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Manages object lifetime by refence counting.
    /// </summary>
    public class DisposeByRefObject : Disposable, IReferenceCounted
    {
        private int FRefCount = 1;

        /// <summary>
        /// See <see cref="Disposable.Dispose(bool)"/>
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            Interlocked.Exchange(ref FRefCount, 0);
            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// See <see cref="Disposable"/>.
        /// </summary>
        protected new void CheckDisposed() 
        {
            if (FRefCount == 0) throw new ObjectDisposedException(null);
        }

        /// <summary>
        /// The current reference count.
        /// </summary>
        public int RefCount => FRefCount;

        /// <summary>
        /// Increments the reference counter.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int AddRef()
        {
            CheckDisposed();
            return Interlocked.Increment(ref FRefCount);
        }

        /// <summary>
        /// Decrements the reference counter.
        /// </summary>
        /// <remarks>If reference count reaches the zero the object will be disposed.</remarks>
        /// <returns>The current reference count.</returns>
        public int Release()
        {
            CheckDisposed();
            int current = Interlocked.Decrement(ref FRefCount);
            if (current == 0) Dispose();
            return current;
        }
    }
}
