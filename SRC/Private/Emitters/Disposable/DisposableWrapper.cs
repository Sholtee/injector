/********************************************************************************
* DisposableWrapper.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Defines the base class for wrapping disposable objects.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    public abstract class DisposableWrapper<T> : DuckBase<T>, IDisposableEx
    {
        private bool FDisposed;

        /// <summary>
        /// Creates a new <see cref="DisposableWrapper{T}"/> instance.
        /// </summary>
        /// <param name="target">The target of this instance.</param>
        public DisposableWrapper(T target) : base(target)
        {
        }

        /// <summary>
        /// See <see cref="IDisposableEx"/>.
        /// </summary>
        bool IDisposableEx.Disposed => FDisposed;

        /// <summary>
        /// See <see cref="IDisposable"/>.
        /// </summary>
        void IDisposable.Dispose()
        {
            //
            // Ne "T"-n legyen megszigoritas h IDiposable leszarmazott legyen mert T lehet interface ami nem
            // leszarmazott, viszont az implementacio megvalisotja.
            //

            (Target as IDisposable)?.Dispose();
            FDisposed = true;
        }
    }
}
