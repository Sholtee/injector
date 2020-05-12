/********************************************************************************
* SafeEnumerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    internal sealed class SafeEnumerator<T> : Disposable, IEnumerator<T>
    {
        private readonly IEnumerator<T> FUnderlyingEnumerator;
        private readonly ReaderWriterLockSlim FLock;

        public SafeEnumerator(IEnumerable<T> src, ReaderWriterLockSlim @lock) 
        {
            @lock.EnterReadLock();
            FLock = @lock;

            FUnderlyingEnumerator = src.GetEnumerator();                            
        }

        public T Current => FUnderlyingEnumerator.Current;

        object? IEnumerator.Current => FUnderlyingEnumerator.Current;

        protected override void Dispose(bool disposeManaged)
        {
            //
            // Ne ellenorizzuk a "disposeManaged" erteket h a lock-ot akkor is eleresszuk 
            // ha a GetEnumerator() a konstruktorban kivetelt dobott.
            //

            FUnderlyingEnumerator?.Dispose();
            FLock.ExitReadLock();
        }

        public bool MoveNext() => FUnderlyingEnumerator.MoveNext();

        public void Reset() => FUnderlyingEnumerator.Reset();
    }
}
