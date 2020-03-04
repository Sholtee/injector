﻿/********************************************************************************
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
        private sealed class LockScope : Disposable
        {
            private readonly Action FReleaseAction;

            public LockScope(Action acquireAction, Action releaseAction)
            {
                acquireAction();
                FReleaseAction = releaseAction;
            }

            protected override void Dispose(bool disposeManaged)
            {
                FReleaseAction();
                base.Dispose(disposeManaged);
            }
        }
        #endregion

        /// <summary>
        /// Enters the lock in write mode.
        /// </summary>
        public static IDisposable AcquireWriterLock(this ReaderWriterLockSlim src) => new LockScope(src.EnterWriteLock, src.ExitWriteLock);

        /// <summary>
        /// Enters the lock in read mode.
        /// </summary>
        public static IDisposable AcquireReaderLock(this ReaderWriterLockSlim src) =>new LockScope(src.EnterReadLock, src.ExitReadLock);
    }
}
