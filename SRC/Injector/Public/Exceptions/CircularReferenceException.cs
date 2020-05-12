/********************************************************************************
* CircularReferenceException.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI
{
    using Internals;
    using Properties;

    /// <summary>
    /// The exception that is thrown on circular reference.
    /// </summary>
    public sealed class CircularReferenceException: Exception
    {
        /// <summary>
        /// Creates a new <see cref="CircularReferenceException"/> instance.
        /// </summary>
        /// <param name="path">The current path on which the circular reference was found.</param>
        internal CircularReferenceException(IEnumerable<IServiceId> path) : this
        (
            string.Format
            (
                Resources.Culture,
                Resources.CIRCULAR_REFERENCE,
                string.Join(" -> ", path.Select(IServiceIdExtensions.FriendlyName))
            )
        ) 
        {
            Data.Add(nameof(path), path);
        }

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
    }
}
