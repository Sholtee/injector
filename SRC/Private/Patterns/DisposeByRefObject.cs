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
    public class DisposeByRefObject : Disposable, IDisposeByRef
    {
        private readonly object FLock = new object();

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
                Ensure.NotDisposed(this);
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
                Ensure.NotDisposed(this);
                if (--RefCount == 0) Dispose();
                return RefCount;
            }
        }
    }
}
