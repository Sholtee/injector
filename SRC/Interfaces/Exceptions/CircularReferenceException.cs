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
    /// <remarks>
    /// Circular reference is the state when there is a circle in the dependency graph. For instance:
    /// <br/>
    /// Svc_1 &#x2B62; Svc_2 &#x2B62; Svc_1
    /// <br/>
    /// Depending on the <see cref="ScopeOptions.ServiceResolutionMode"/> property this exception can be thrown either in compilation time or on service request.
    /// </remarks>
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
