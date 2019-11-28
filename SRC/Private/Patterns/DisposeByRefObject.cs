/********************************************************************************
* DisposeByRefObject.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Manages object lifetime by refence counting.
    /// </summary>
    public class DisposeByRefObject : Disposable, IReferenceCounted
    {
        private readonly object FLock = new object();

        /// <summary>
        /// The current reference count.
        /// </summary>
        public int RefCount { get; private set; } = 1;

        /// <summary>
        /// Increments the reference counter.
        /// </summary>
        /// <returns>The current reference count.</returns>
        public int AddRef()
        {
            lock (FLock)
            {
                CheckDisposed();
                return ++RefCount;
            }
        }

        /// <summary>
        /// Decrements the reference counter.
        /// </summary>
        /// <remarks>If reference count reaches the zero the object will be disposed.</remarks>
        /// <returns>The current reference count.</returns>
        public int Release()
        {
            lock (FLock)
            {
                CheckDisposed();
                if (--RefCount == 0) Dispose();
                return RefCount;
            }
        }
    }
}
