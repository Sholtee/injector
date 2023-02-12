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
        public CircularReferenceException(string message, IReadOnlyList<AbstractServiceEntry> circle): base(message)
            => Circle = circle ?? throw new ArgumentNullException(nameof(circle));

        /// <summary>
        /// The circle itself.
        /// </summary>
        public IReadOnlyList<AbstractServiceEntry> Circle { get; }
    }
}
