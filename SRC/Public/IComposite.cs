/********************************************************************************
* IComposite.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    public interface IComposite<T>: IDisposable where T : IComposite<T>
    {
        /// <summary>
        /// The parent of this entity.
        /// </summary>
        T Parent { get; }

        /// <summary>
        /// All children of this entity.
        /// </summary>
        ICollection<T> Children { get; }

        /// <summary>
        /// Creates a new child entity and puts into the children list.
        /// </summary>
        /// <returns></returns>
        T CreateChild();

        /// <summary>
        /// Whether the entity has a parent or not.
        /// </summary>
        bool IsRoot { get; }
    }
}