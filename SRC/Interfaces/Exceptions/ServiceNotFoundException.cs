/********************************************************************************
* ServiceNotFoundException.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// The exception that is thrown when a service could not be found.
    /// </summary>
    public sealed class ServiceNotFoundException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="ServiceNotFoundException"/> instance.
        /// </summary>
        public ServiceNotFoundException(string message, AbstractServiceEntry? requestor, MissingServiceEntry requested) : base(message)
        {
            Requestor = requestor;
            Requested = requested ?? throw new ArgumentNullException(nameof(requested));
        }

        /// <summary>
        /// The dependant service which requested a missing service.
        /// </summary>
        /// <remarks>This property is null if the request was initiated from user code: <code>scope.Get&lt;IMissingService&gt;()</code></remarks>
        public AbstractServiceEntry? Requestor { get; }

        /// <summary>
        /// The requested service.
        /// </summary>
        public MissingServiceEntry Requested { get; }
    }
}
