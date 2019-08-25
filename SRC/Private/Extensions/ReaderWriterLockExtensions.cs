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
            private readonly Action FMReleaseAction;

            public Lock(Action acquireAction, Action releaseAction)
            {
                acquireAction();
                FMReleaseAction = releaseAction;
            }

            protected override void Dispose(bool disposeManaged)
            {
                FMReleaseAction();
                base.Dispose(disposeManaged);
            }
        }
        #endregion

        /// <summary>
        /// író lock elkérése.
        /// </summary>
        public static IDisposable AcquireWriterLockSmart(this ReaderWriterLockSlim src) => new Lock(src.EnterWriteLock, src.ExitWriteLock);

        /// <summary>
        /// Olvasó lock elkérése.
        /// </summary>
        public static IDisposable AcquireReaderLockSmart(this ReaderWriterLockSlim src) =>new Lock(src.EnterReadLock, src.ExitReadLock);
    }
}
