/********************************************************************************
* IComposite.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes the composite pattern.
    /// </summary>
    /// <typeparam name="T">The type on which we want to apply the pattern.</typeparam>
    /// <remarks>This is an internal interface so it may change from version to version. Don't use it!</remarks>
    public interface IComposite<T>: IDisposable, IAsyncDisposable where T : IComposite<T>
    {
        /// <summary>
        /// The parent of this entity.
        /// </summary>
        T Parent { get; set; }

        /// <summary>
        /// The children of this entity.
        /// </summary>
        IReadOnlyCollection<T> Children { get; }

        /// <summary>
        /// Adds a new child to the <see cref="Children"/> list.
        /// </summary>
        /// <param name="child">The child being added.</param>
        /// <exception cref="InvalidOperationException">The <paramref name="child"/> has already been added, the parent not matching or the count of <see cref="Children"/> reached the limit that was defined in the <see cref="Config"/>.</exception>
        void AddChild(T child);

        /// <summary>
        /// Removes the given child.
        /// </summary>
        /// <param name="child">The child to be removed.</param>
        /// <remarks>The entry being removed will not be disposed.</remarks>
        /// <exception cref="InvalidOperationException">The child is not contained by the parent on which the operation was performed.</exception>
        void RemoveChild(T child);
    }
}