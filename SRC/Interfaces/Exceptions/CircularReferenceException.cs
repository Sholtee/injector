/********************************************************************************
* CircularReferenceException.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// The exception that is thrown on circular reference.
    /// </summary>
    public sealed class CircularReferenceException: Exception
    {
        /// <summary>
        /// Creates a new <see cref="CircularReferenceException"/> instance.
        /// </summary>
        public CircularReferenceException()
        {
        }

        /// <summary>
        /// Creates a new <see cref="CircularReferenceException"/> instance.
        /// </summary>
        public CircularReferenceException(string message) : this(message, null!)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CircularReferenceException"/> instance.
        /// </summary>
        public CircularReferenceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// The circle
        /// </summary>
        public IReadOnlyList<AbstractServiceEntry>? Circle { get; init; }
    }
}
