/********************************************************************************
* CircularReferenceException.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
        public CircularReferenceException(IEnumerable<(Type Interface, string Name)> path) : base(string.Format(
            CultureInfo.CurrentCulture,
            Resources.CIRCULAR_REFERENCE,
            string.Join(" -> ", path.Select(cp => cp.FriendlyName())))) 
        {
            Data.Add(nameof(path), path);
        }
    }
}
