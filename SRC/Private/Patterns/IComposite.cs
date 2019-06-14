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
    /// <typeparam name="T">The type on which we want to apply the composite pattern.</typeparam>
    /// <remarks>This is an internal interface so it can be changed from version to version. Don't use it!</remarks>
    public interface IComposite<out T>: IDisposable where T : IComposite<T>
    {
        /// <summary>
        /// The parent of this entity.
        /// </summary>
        T Parent { get; }

        /// <summary>
        /// All children of this entity.
        /// </summary>
        IReadOnlyCollection<T> Children { get; }

        /// <summary>
        /// Creates a new child entity and puts it into the <see cref="Children"/> list.
        /// </summary>
        /// <returns>The newly created child</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>
        /// <description>The child created by this method should inherit the state of this entity (but modifying the child should not affect it).</description>
        /// </item>
        /// <item>
        /// <description>Disposing the child should cause its removal from the <see cref="Children"/> list. Without it, this entity is responsible for freeing it on dispose.</description>
        /// </item>
        /// </list>
        /// </remarks>
        T CreateChild();

        /// <summary>
        /// Whether the entity has a parent or not.
        /// </summary>
        bool IsRoot { get; }
    }
}