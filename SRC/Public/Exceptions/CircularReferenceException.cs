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
        public CircularReferenceException(IEnumerable<(Type Interface, string Name)> path) : base(string.Format(
            Resources.CIRCULAR_REFERENCE,
            string.Join(" -> ", path.Select(cp => cp.FriendlyName())))) {}
    }
}
