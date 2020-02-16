/********************************************************************************
* CircularReferenceException.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Solti.Utils.DI
{
    using Internals;
    using Properties;

    /// <summary>
    /// The exception that is thrown on circular reference.
    /// </summary>
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Path must be passed.")]
    public sealed class CircularReferenceException: Exception
    {
        /// <summary>
        /// Creates a new <see cref="CircularReferenceException"/> instance.
        /// </summary>
        /// <param name="path">The current path on which the circular reference was found.</param>
        internal CircularReferenceException(IEnumerable<IServiceId> path) : base
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
    }
}
