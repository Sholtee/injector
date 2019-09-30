/********************************************************************************
*  ReaderWriterLockExtensions.cs                                                *
*                                                                               *
*  Author: Denes Solti                                                          *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    internal static class ReaderWriterLockExtensions
    {
        #region Helpers
        private sealed class Lock : Disposable
        {
            private readonly Action FReleaseAction;

            public Lock(Action acquireAction, Action releaseAction)
            {
                acquireAction();
                FReleaseAction = releaseAction;
            }

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged) FReleaseAction();
                base.Dispose(disposeManaged);
            }
        }
        #endregion

        /// <summary>
        /// író lock elkérése.
        /// </summary>
        public static IDisposable AcquireWriterLock(this ReaderWriterLockSlim src) => new Lock(src.EnterWriteLock, src.ExitWriteLock);

        /// <summary>
        /// Olvasó lock elkérése.
        /// </summary>
        public static IDisposable AcquireReaderLock(this ReaderWriterLockSlim src) =>new Lock(src.EnterReadLock, src.ExitReadLock);
    }
}
